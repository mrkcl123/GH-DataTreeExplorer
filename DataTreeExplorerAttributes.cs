using System;
using System.Collections.Generic;
using System.Drawing;
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

        // Object representing anything we draw in our file explorer list
        private class UIElement
        {
            public bool IsFolder;       // True = Directory node (e.g. {0}), False = Data branch or raw data item
            public bool IsDataLeaf;     // True = The final end branch containing items (e.g. {0;1})
            public string Label;        // Text displayed (e.g. "0", "{0;1}", or "[0] Line")
            public string NodeKey;      // Unique structural key (e.g. "{0}" or "{0;1}")
            public int Indent;          // Hierarchy depth multiplier
            public int BranchIndex = -1;// Matches the physical GH output param if a leaf node
            public int ItemCount;       // Total sub-items inside
        }

        // Internal tree node representation for path sorting
        private class PathNode
        {
            public string Name;
            public string FullKey;
            public int BranchIndex = -1;
            public Dictionary<string, PathNode> SubFolders = new Dictionary<string, PathNode>();
        }

        private void RegenerateTreeLayout()
        {
            _visibleElements.Clear();
            if (Owner.CurrentPaths == null || Owner.CurrentPaths.Count == 0) return;

            // 1. Construct an internal directory system
            PathNode rootNode = new PathNode { Name = "Root", FullKey = "Root" };
            
            for (int i = 0; i < Owner.CurrentPaths.Count; i++)
            {
                GH_Path path = Owner.CurrentPaths[i];
                PathNode current = rootNode;
                string structuredKey = "";

                for (int j = 0; j < path.Length; j++)
                {
                    string indexStr = path.Indices[j].ToString();
                    structuredKey = j == 0 ? $"{{{indexStr}}}" : structuredKey.TrimEnd('}') + ";" + indexStr + "}";

                    if (!current.SubFolders.ContainsKey(indexStr))
                    {
                        current.SubFolders[indexStr] = new PathNode { Name = indexStr, FullKey = structuredKey };
                    }
                    current = current.SubFolders[indexStr];
                }
                current.BranchIndex = i; // Mark this folder node as an actual data holder leaf
            }

            // 2. Flatten out the tree system sequentially based on expansion rules
            FlattenNode(rootNode, 0);
        }

        private void FlattenNode(PathNode node, int currentIndent)
        {
            // Skip rendering the dummy root wrapper directly, dive straight into children
            if (node.FullKey == "Root")
            {
                foreach (var child in node.SubFolders.Values) FlattenNode(child, currentIndent);
                return;
            }

            bool isOpen = Owner.ExpandedFolders.Contains(node.FullKey);
            bool isDataLeaf = node.BranchIndex != -1;
            bool isFolder = node.SubFolders.Count > 0;

            int itemsInBranch = 0;
            if (isDataLeaf) itemsInBranch = Owner.CurrentTree.Branches[node.BranchIndex].Count;

            _visibleElements.Add(new UIElement
            {
                IsFolder = isFolder || isDataLeaf,
                IsDataLeaf = isDataLeaf,
                Label = isDataLeaf ? node.FullKey : node.Name,
                NodeKey = node.FullKey,
                Indent = currentIndent,
                BranchIndex = node.BranchIndex,
                ItemCount = itemsInBranch
            });

            if (isOpen)
            {
                // Render nested sub-folders first
                foreach (var child in node.SubFolders.Values)
                {
                    FlattenNode(child, currentIndent + 1);
                }

                // If this specific node also explicitly carries raw list elements, inject item previews
                if (isDataLeaf)
                {
                    var branchList = Owner.CurrentTree.Branches[node.BranchIndex];
                    for (int j = 0; j < branchList.Count; j++)
                    {
                        string preview = branchList[j] != null ? branchList[j].ToString() : "null";
                        if (preview.Length > 40) preview = preview.Substring(0, 37) + "...";

                        _visibleElements.Add(new UIElement
                        {
                            IsFolder = false,
                            IsDataLeaf = false,
                            Label = $"[{j}] {preview}",
                            NodeKey = node.FullKey + $"_item_{j}",
                            Indent = currentIndent + 1
                        });
                    }
                }
            }
        }

        protected override void Layout()
        {
            base.Layout();
            RegenerateTreeLayout();

            float extraHeight = _visibleElements.Count * _rowHeight;
            
            RectangleF rec = Bounds;
            rec.Width = 200f; // Extra width to hold deeper indent variations comfortably
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

            // Sync physical connection knots only to nodes representing true final branch paths
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
                
                for (int i = 0; i < _visibleElements.Count; i++)
                {
                    var element = _visibleElements[i];
                    float indentOffset = element.Indent * 14f;
                    RectangleF rowRect = new RectangleF(Bounds.X + 5 + indentOffset, currentY, Bounds.Width - 10 - indentOffset, _rowHeight);

                    if (element.IsFolder)
                    {
                        bool isFolderOpen = Owner.ExpandedFolders.Contains(element.NodeKey);
                        PointF[] chevron = GetChevronPoints(rowRect.X + 4, rowRect.Y + 6, isFolderOpen);
                        graphics.FillPolygon(Brushes.Black, chevron);

                        StringFormat leftAlign = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                        RectangleF textRect = new RectangleF(rowRect.X + 16, rowRect.Y, Bounds.Width - 100, _rowHeight);
                        
                        // Bold folder headers vs regular branch endpoints
                        Font labelFont = element.IsDataLeaf ? GH_FontServer.Standard : GH_FontServer.StandardBold;
                        graphics.DrawString(element.Label, labelFont, Brushes.Black, textRect, leftAlign);

                        if (element.IsDataLeaf)
                        {
                            StringFormat rightAlign = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                            RectangleF countLabelRect = new RectangleF(Bounds.X + 5, rowRect.Y, Bounds.Width - 15, _rowHeight);
                            string text = element.ItemCount == 1 ? "1 Item" : $"{element.ItemCount} Items";
                            graphics.DrawString(text, GH_FontServer.StandardItalic, Brushes.Gray, countLabelRect, rightAlign);
                        }
                    }
                    else
                    {
                        // Indented primitive raw list objects
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

                    if (hitBox.Contains(e.CanvasLocation) && element.IsFolder)
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