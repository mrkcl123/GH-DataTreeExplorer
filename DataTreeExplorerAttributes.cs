using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

public class DataTreeExplorerAttributes : GH_ComponentAttributes
{
    // Link back to the main component to access its data and state
    private DataTreeExplorerComponent Owner => (DataTreeExplorerComponent)OwnerComponent;

    public DataTreeExplorerAttributes(DataTreeExplorerComponent owner) : base(owner) { }

    // 1. Layout: This determines the size and shape of the component
    protected override void Layout()
    {
        // Start with the standard component layout
        base.Layout();

        // Adjust the height based on the number of branches
        // We add 20 pixels for every branch row
        float rowHeight = 20f;
        float totalRowsHeight = Owner.Params.Output.Count * rowHeight;
        
        // Update the Bounds of the component
        RectangleF rec = Bounds;
        rec.Height += totalRowsHeight; 
        Bounds = rec;
    }

    // 2. Render: This draws the actual UI (Chevrons and Text)
    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
        // Draw the default component capsule first
        base.Render(canvas, graphics, channel);

        // Only draw our custom UI during the 'Objects' channel
        if (channel == GH_CanvasChannel.Objects)
        {
            float rowHeight = 20f;
            float currentY = Bounds.Y + 50f; // Start below the default input/output area

            for (int i = 0; i < Owner.Params.Output.Count; i++)
            {
                // Draw Row Background
                RectangleF rowRect = new RectangleF(Bounds.X + 5, currentY, Bounds.Width - 10, rowHeight);
                
                // Draw Chevron
                bool isOpen = (i < Owner.BranchStates.Count) && Owner.BranchStates[i];
                PointF[] chevron = GetChevronPoints(rowRect.X + 5, rowRect.Y + 5, isOpen);
                graphics.FillPolygon(Brushes.Black, chevron);

                // Draw Path Label (e.g., {0;0})
                string pathLabel = Owner.Params.Output[i].NickName;
                graphics.DrawString(pathLabel, GH_FontServer.Standard, Brushes.DarkSlateGray, rowRect.X + 20, rowRect.Y + 2);

                currentY += rowHeight;
            }
        }
    }

    // 3. Interaction: Handle the mouse clicks on chevrons
    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left)
        {
            float rowHeight = 20f;
            float startY = Bounds.Y + 50f;

            for (int i = 0; i < Owner.Params.Output.Count; i++)
            {
                // Define the hit-box for the chevron
                RectangleF chevronHitBox = new RectangleF(Bounds.X + 5, startY + (i * rowHeight), 20, 20);

                if (chevronHitBox.Contains(e.CanvasLocation))
                {
                    // Toggle the state in the main component
                    if (i < Owner.BranchStates.Count)
                    {
                        Owner.BranchStates[i] = !Owner.BranchStates[i];
                        
                        // Force a redraw and update
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
            }
        }
        return base.RespondToMouseDown(sender, e);
    }

    // Helper to generate triangle points for the chevron
    private PointF[] GetChevronPoints(float x, float y, bool isOpen)
    {
        if (isOpen) // Triangle pointing Down
        {
            return new PointF[] { new PointF(x, y + 2), new PointF(x + 10, y + 2), new PointF(x + 5, y + 8) };
        }
        else // Triangle pointing Right
        {
            return new PointF[] { new PointF(x + 2, y), new PointF(x + 2, y + 10), new PointF(x + 8, y + 5) };
        }
    }
}