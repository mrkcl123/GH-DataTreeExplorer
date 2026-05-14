using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace DataTreeExplorer
{
    public class DataTreeExplorerAttributes : GH_ComponentAttributes
    {
        public new DataTreeExplorerComponent Owner => (DataTreeExplorerComponent)base.Owner;

        public DataTreeExplorerAttributes(DataTreeExplorerComponent owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();

            float rowHeight = 22f;
            int rowCount = Owner.Params.Output.Count;
            float extraHeight = rowCount * rowHeight;
            
            RectangleF rec = Bounds;
            // Fall back to a normal component height (60px) if no branches are active
            rec.Height = 60f + extraHeight; 
            Bounds = rec;

            System.Reflection.FieldInfo innerField = typeof(GH_ComponentAttributes).GetField("m_innerBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (innerField != null) innerField.SetValue(this, Bounds);

            float currentY = Bounds.Y + 50f;
            for (int i = 0; i < Owner.Params.Output.Count; i++)
            {
                IGH_Param param = Owner.Params.Output[i];
                param.Attributes.Pivot = new PointF(Bounds.Right, currentY + (rowHeight / 2));
                param.Attributes.Bounds = new RectangleF(Bounds.Right - 5, currentY, 10, rowHeight);
                currentY += rowHeight;
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
                
                foreach (IGH_Param input in Owner.Params.Input)
                {
                    capsule.AddInputGrip(input.Attributes.Pivot.Y);
                }
                
                foreach (IGH_Param output in Owner.Params.Output)
                {
                    capsule.AddOutputGrip(output.Attributes.Pivot.Y);
                }
                
                capsule.Render(graphics, Selected, Owner.Locked, false);
                capsule.Dispose();

                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                RectangleF titleRect = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, 20);
                graphics.DrawString(Owner.Name, GH_FontServer.StandardBold, Brushes.Black, titleRect, format);

                float rowHeight = 22f;
                float currentY = Bounds.Y + 50f;

                for (int i = 0; i < Owner.Params.Output.Count; i++)
                {
                    RectangleF rowRect = new RectangleF(Bounds.X + 5, currentY, Bounds.Width - 10, rowHeight);
                    
                    bool isOpen = (i < Owner.BranchStates.Count) && Owner.BranchStates[i];
                    PointF[] chevron = GetChevronPoints(rowRect.X + 6, rowRect.Y + 6, isOpen);
                    graphics.FillPolygon(Brushes.Black, chevron);
                    
                    StringFormat rightAlign = new StringFormat();
                    rightAlign.Alignment = StringAlignment.Far;
                    rightAlign.LineAlignment = StringAlignment.Center;
                    RectangleF textRect = new RectangleF(rowRect.X, rowRect.Y, rowRect.Width - 10, rowRect.Height);
                    
                    graphics.DrawString(Owner.Params.Output[i].NickName, GH_FontServer.Standard, Brushes.Black, textRect, rightAlign);
                    
                    if (i == 0) graphics.DrawString("Tree", GH_FontServer.Standard, Brushes.Black, Bounds.X + 10, currentY - 30);

                    currentY += rowHeight;
                }
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                float rowHeight = 22f;
                float startY = Bounds.Y + 50f;
                for (int i = 0; i < Owner.Params.Output.Count; i++)
                {
                    RectangleF hitBox = new RectangleF(Bounds.X, startY + (i * rowHeight), 30, rowHeight);
                    if (hitBox.Contains(e.CanvasLocation))
                    {
                        Owner.BranchStates[i] = !Owner.BranchStates[i];
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
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