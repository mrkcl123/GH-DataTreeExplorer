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
        // Use 'base.Owner' to access the component the attributes belong to
        public new DataTreeExplorerComponent Owner => (DataTreeExplorerComponent)base.Owner;

        public DataTreeExplorerAttributes(DataTreeExplorerComponent owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            
            float rowHeight = 20f;
            // Add extra height for the branch list
            float totalRowsHeight = Owner.Params.Output.Count * rowHeight;
            
            RectangleF rec = Bounds;
            rec.Height += totalRowsHeight; 
            Bounds = rec;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            // Render the standard component look
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                float rowHeight = 20f;
                float currentY = Bounds.Y + 50f;

                for (int i = 0; i < Owner.Params.Output.Count; i++)
                {
                    RectangleF rowRect = new RectangleF(Bounds.X + 5, currentY, Bounds.Width - 10, rowHeight);
                    
                    // Draw the Chevron
                    bool isOpen = (i < Owner.BranchStates.Count) && Owner.BranchStates[i];
                    PointF[] chevron = GetChevronPoints(rowRect.X + 5, rowRect.Y + 5, isOpen);
                    graphics.FillPolygon(System.Drawing.Brushes.Black, chevron);
                    
                    // Draw the NickName of the output parameter
                    string label = Owner.Params.Output[i].NickName;
                    graphics.DrawString(label, GH_FontServer.Standard, System.Drawing.Brushes.Black, rowRect.X + 20, rowRect.Y + 2);
                    
                    currentY += rowHeight;
                }
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                float rowHeight = 20f;
                float startY = Bounds.Y + 50f;

                for (int i = 0; i < Owner.Params.Output.Count; i++)
                {
                    RectangleF chevronHitBox = new RectangleF(Bounds.X + 5, startY + (i * rowHeight), 20, 20);
                    
                    if (chevronHitBox.Contains(e.CanvasLocation))
                    {
                        if (i < Owner.BranchStates.Count)
                        {
                            Owner.BranchStates[i] = !Owner.BranchStates[i];
                            Owner.ExpireSolution(true);
                            return GH_ObjectResponse.Handled;
                        }
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private PointF[] GetChevronPoints(float x, float y, bool isOpen)
        {
            if (isOpen) 
                return new PointF[] { new PointF(x, y + 2), new PointF(x + 10, y + 2), new PointF(x + 5, y + 8) };
            
            return new PointF[] { new PointF(x + 2, y), new PointF(x + 2, y + 10), new PointF(x + 8, y + 5) };
        }
    }
}