#pragma warning disable CA1416 // Disable platform compatibility warnings for System.Drawing on non-Windows systems

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;

namespace DataTreeExplorer
{
    public class DataTreeExplorerAttributes : GH_ComponentAttributes
    {
        public new DataTreeExplorerComponent Owner => (DataTreeExplorerComponent)base.Owner;

        private List<UIElement> _visibleElements = new List<UIElement>();
        private float _rowHeight = 22f;
        private float _contentStartY = 72f;

        public DataTreeExplorerAttributes(DataTreeExplorerComponent owner) : base(owner) { }

        private class UIElement
        {
            public bool IsHeaderGroup;  
            public bool IsDataLeaf;     
            public string Label;        
            public string RightLabel;   
            public string NodeKey;      
            public int Indent;          
            public int BranchIndex = -1;
        }

        private class FolderGroup
        {
            public string Key;
            public List<int> LeafBranchIndices = new List<int>();
        }

        private void RegenerateTreeLayout()
        {
            _visibleElements.Clear();
            if (Owner.CurrentPaths == null || Owner.CurrentPaths.Count == 0) return;

            var groups = new Dictionary<string, FolderGroup>();
            var standaloneLeaves = new List<int>();

            for (int i = 0; i < Owner.CurrentPaths.Count; i++)
            {
                GH_Path path = Owner.CurrentPaths[i];
                
                if (path.Length > 1)
                {
                    List<string> parentParts = new List<string>();
                    for (int j = 0; j < path.Length - 1; j++)
                    {
                        parentParts.Add(path.Indices[j].ToString());
                    }
                    string groupKey = string.Join(";", parentParts);

                    if (!groups.ContainsKey(groupKey))
                    {
                        groups[groupKey] = new FolderGroup { Key = groupKey };
                    }
                    groups[groupKey].LeafBranchIndices.Add(i);
                }
                else
                {
                    standaloneLeaves.Add(i);
                }
            }

            // FIX: Renamed "[Root Branches]" to unified style "[X]"
            if (standaloneLeaves.Count > 0)
            {
                _visibleElements.Add(new UIElement
                {
                    IsHeaderGroup = true,
                    IsDataLeaf = false,
                    Label = "[X]",
                    RightLabel = $"{standaloneLeaves.Count} {(standaloneLeaves.Count == 1 ? "Branch" : "Branches")}",
                    NodeKey = "heading_standalone_root",
                    Indent = 0
                });

                foreach (int idx in standaloneLeaves.OrderBy(idx => Owner.CurrentPaths[idx]))
                {
                    AddDataLeafToUI(idx, 1); 
                }
            }

            // Deep Groups
            foreach (var group in groups.Values.OrderBy(g => g.Key))
            {
                int branchCount = group.LeafBranchIndices.Count;
                string headingLabel = $"[{group.Key};X]";
                string headingRightLabel = $"{branchCount} {(branchCount == 1 ? "Branch" : "Branches")}";

                _visibleElements.Add(new UIElement
                {
                    IsHeaderGroup = true,
                    IsDataLeaf = false,
                    Label = headingLabel,
                    RightLabel = headingRightLabel,
                    NodeKey = "heading_" + group.Key,
                    Indent = 0
                });

                var sortedIndices = group.LeafBranchIndices.OrderBy(idx => Owner.CurrentPaths[idx]).ToList();
                foreach (int idx in sortedIndices)
                {
                    AddDataLeafToUI(idx, 1);
                }
            }
        }

        private void AddDataLeafToUI(int branchIndex, int indentLevel)
        {
            GH_Path path = Owner.CurrentPaths[branchIndex];
            string pathKey = path.ToString();
            bool isLeafOpen = Owner.ExpandedFolders.Contains(pathKey);
            int itemsInBranch = Owner.CurrentTree.Branches[branchIndex].Count;
            string itemsText = itemsInBranch == 1 ? "1 Item" : $"{itemsInBranch} Items";

            _visibleElements.Add(new UIElement
            {
                IsHeaderGroup = false,
                IsDataLeaf = true,
                Label = pathKey,
                RightLabel = itemsText,
                NodeKey = pathKey,
                Indent = indentLevel,
                BranchIndex = branchIndex
            });

            if (isLeafOpen && itemsInBranch > 0)
            {
                var branchList = Owner.CurrentTree.Branches[branchIndex];
                for (int j = 0; j < branchList.Count; j++)
                {
                    string preview = branchList[j] != null ? branchList[j].ToString() : "null";
                    if (preview.Length > 40) preview = preview.Substring(0, 37) + "...";

                    _visibleElements.Add(new UIElement
                    {
                        IsHeaderGroup = false,
                        IsDataLeaf = false,
                        Label = $"[{j}] {preview}",
                        RightLabel = string.Empty,
                        NodeKey = pathKey + $"_item_{j}",
                        Indent = indentLevel + 1
                    });
                }
            }
        }

        protected override void Layout()
        {
            base.Layout();
            RegenerateTreeLayout();

            float extraHeight = _visibleElements.Count * _rowHeight;
            
            RectangleF rec = Bounds;
            rec.Width = 240f; 
            rec.Height = _contentStartY + extraHeight; 
            Bounds = rec;

            System.Reflection.FieldInfo innerField = typeof(GH_ComponentAttributes).GetField("m_innerBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (innerField != null) innerField.SetValue(this, Bounds);

            if (Owner.Params.Input.Count > 0)
            {
                var inputParam = Owner.Params.Input[0];
                inputParam.Attributes.Pivot = new PointF(Bounds.Left, Bounds.Y + 50f + (_rowHeight / 2));
                inputParam.Attributes.Bounds = new RectangleF(Bounds.Left, Bounds.Y + 50f, 10, _rowHeight);
            }

            float currentY = Bounds.Y + _contentStartY;
            foreach (var element in _visibleElements)
            {
                if (element.IsDataLeaf)
                {
                    IGH_Param param = Owner.Params.Output[element.BranchIndex];
                    param.Attributes.Pivot = new PointF(Bounds.Right, currentY + (_rowHeight / 2));
                    param.Attributes.Bounds = new RectangleF(Bounds.Right - 5, currentY, 10, _rowHeight);
                }
                currentY += _rowHeight;
            }
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires || channel == GH_CanvasChannel.Overlay)
            {
                base.Render(canvas, graphics, channel);
                return;
            }

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Normal);
                foreach (IGH_Param input in Owner.Params.Input) capsule.AddInputGrip(input.Attributes.Pivot.Y);
                foreach (IGH_Param output in Owner.Params.Output) capsule.AddOutputGrip(output.Attributes.Pivot.Y);
                
                capsule.Render(graphics, Selected, Owner.Locked, false);
                capsule.Dispose();

                StringFormat titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                RectangleF titleRect = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, 20);
                graphics.DrawString(Owner.Name, GH_FontServer.StandardBold, Brushes.Black, titleRect, titleFormat);

                StringFormat inputLabelFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                RectangleF inputLabelRect = new RectangleF(Bounds.X + 12, Bounds.Y + 50f, Bounds.Width - 24, _rowHeight);
                graphics.DrawString("Tree", GH_FontServer.StandardBold, Brushes.Black, inputLabelRect, inputLabelFormat);

                graphics.DrawLine(Pens.LightGray, Bounds.X + 5, Bounds.Y + _contentStartY - 2, Bounds.Right - 5, Bounds.Y + _contentStartY - 2);

                float currentY = Bounds.Y + _contentStartY;
                Color darkGray = Color.FromArgb(64, 64, 64);
                
                for (int i = 0; i < _visibleElements.Count; i++)
                {
                    var element = _visibleElements[i];
                    float indentOffset = element.Indent * 14f;
                    RectangleF rowRect = new RectangleF(Bounds.X + 5 + indentOffset, currentY, Bounds.Width - 10 - indentOffset, _rowHeight);

                    StringFormat leftAlign = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    StringFormat rightAlign = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                    RectangleF rightLabelRect = new RectangleF(Bounds.X + 5, rowRect.Y, Bounds.Width - 15, _rowHeight);

                    if (element.IsHeaderGroup)
                    {
                        RectangleF textRect = new RectangleF(rowRect.X, rowRect.Y, Bounds.Width - 110, _rowHeight);
                        using (Brush brush = new SolidBrush(darkGray))
                        {
                            graphics.DrawString(element.Label, GH_FontServer.StandardBold, brush, textRect, leftAlign);
                            graphics.DrawString(element.RightLabel, GH_FontServer.StandardItalic, brush, rightLabelRect, rightAlign);
                        }
                    }
                    else if (element.IsDataLeaf)
                    {
                        bool isOpen = Owner.ExpandedFolders.Contains(element.NodeKey);
                        int branchIndex = element.BranchIndex;
                        int itemsCount = Owner.CurrentTree.Branches[branchIndex].Count;

                        if (itemsCount > 0)
                        {
                            PointF[] chevron = GetChevronPoints(rowRect.X + 4, rowRect.Y + 6, isOpen);
                            graphics.FillPolygon(Brushes.Black, chevron);
                        }

                        RectangleF textRect = new RectangleF(rowRect.X + 16, rowRect.Y, Bounds.Width - 110, _rowHeight);
                        graphics.DrawString(element.Label, GH_FontServer.Standard, Brushes.Black, textRect, leftAlign);
                        graphics.DrawString(element.RightLabel, GH_FontServer.StandardItalic, Brushes.Gray, rightLabelRect, rightAlign);
                    }
                    else
                    {
                        graphics.DrawString(element.Label, GH_FontServer.StandardItalic, Brushes.Gray, rowRect.X + 4, rowRect.Y + 3);
                    }

                    currentY += _rowHeight;
                }
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                float currentY = Bounds.Y + _contentStartY;
                for (int i = 0; i < _visibleElements.Count; i++)
                {
                    var element = _visibleElements[i];
                    RectangleF hitBox = new RectangleF(Bounds.X, currentY, Bounds.Width, _rowHeight);

                    if (hitBox.Contains(e.CanvasLocation) && element.IsDataLeaf && Owner.CurrentTree.Branches[element.BranchIndex].Count > 0)
                    {
                        if (Owner.ExpandedFolders.Contains(element.NodeKey))
                            Owner.ExpandedFolders.Remove(element.NodeKey);
                        else
                            Owner.ExpandedFolders.Add(element.NodeKey);

                        this.Owner.OnPingDocument().ScheduleSolution(5, (doc) => {
                            Owner.ExpireSolution(true);
                        });
                        return GH_ObjectResponse.Handled;
                    }
                    currentY += _rowHeight;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private PointF[] GetChevronPoints(float x, float y, bool isOpen)
        {
            if (isOpen) return new PointF[] { new PointF(x, y + 2), new PointF(x + 10, y + 2), new PointF(x + 5, y + 9) };
            return new PointF[] { new PointF(x + 2, y), new PointF(x + 2, y + 10), new PointF(x + 9, y + 5) };
        }
    }
}