using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DSAproject
{
    public partial class MainForm : Form
    {
        // Dependencies
        InputParser _parser = new InputParser();
        GraphManager _graphManager = new GraphManager();

        // Visualization State
        List<string> _executionOrder;
        int _currentStepIndex = -1; // -1 means not started

        public MainForm()
        {
            InitializeComponent();
            // Enable double buffering on panel for smooth drawing
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnlGraph, new object[] { true });
        }

        // 1. VALIDATE BUTTON CLICK
        private void btnValidate_Click(object sender, EventArgs e)
        {
            try
            {
                // A. Parse
                var rules = _parser.ParseRules(txtInput.Text);

                // B. Build Graph
                _graphManager.BuildGraph(rules);
                // --- START OF NEW CODE ---
                // If the graph is empty (because user typed garbage or nothing), show error
                if (_graphManager.Nodes.Count == 0)
                {
                    lblStatus.Text = "Error: No valid rules found! Use format 'TaskA -> TaskB'";
                    lblStatus.ForeColor = Color.Red;
                    pnlGraph.Invalidate(); // Clear old drawings
                    return; // Stop here, do not continue
                }
                // --- END OF NEW CODE ---

                // C. Calculate Positions for Visualization (Layered Layout)
                AssignCoordinates();

                // D. Try Topological Sort
                _executionOrder = _graphManager.GetExecutionOrder();

                // Success
                lblStatus.Text = "Graph Valid! Click 'Next Step' to visualize.";
                lblStatus.ForeColor = Color.Green;
                btnStep.Enabled = true;
                _currentStepIndex = -1; // Reset stepper
                pnlGraph.Invalidate(); // Trigger repaint
            }
            catch (Exception ex)
            {
               
                // Error / Cycle Handling
                lblStatus.Text = ex.Message;
                lblStatus.ForeColor = Color.Red;

                // Optional: If cycle, visualize the red loop
                var cyclePath = _graphManager.FindCyclePath();
                if (cyclePath.Count > 0)
                {
                    lblStatus.Text += " Cycle: " + string.Join("->", cyclePath);
                }
                pnlGraph.Invalidate();
            }
        }

        // 2. NEXT STEP BUTTON (Animation Logic)
        private void btnStep_Click(object sender, EventArgs e)
        {
            if (_executionOrder != null && _currentStepIndex < _executionOrder.Count - 1)
            {
                _currentStepIndex++;
                pnlGraph.Invalidate(); // Re-draw with new highlight
            }
            else
            {
                MessageBox.Show("Execution Complete!");
            }
        }

        // new method
        private void AssignCoordinates()
        {
            // 1. Get center of the panel
            int centerX = pnlGraph.Width / 2;
            int centerY = pnlGraph.Height / 2;

            // 2. Calculate radius (leave 60px margin from edge)
            int radius = Math.Min(centerX, centerY) - 60;
            if (radius < 50) radius = 50; // Safety for small windows

            int nodeCount = _graphManager.Nodes.Count;
            int i = 0;

            foreach (var node in _graphManager.Nodes.Values)
            {
                // 3. Math to place nodes in a circle
                double angle = 2 * Math.PI * i / nodeCount;
                int x = centerX + (int)(radius * Math.Cos(angle));
                int y = centerY + (int)(radius * Math.Sin(angle));

                // Center the node point (subtract half size of node)
                node.Location = new Point(x - 20, y - 20);
                i++;
            }
        }

        //old method:

        // 3. VISUALIZATION LOGIC (The "Wow" Factor)
        //private void AssignCoordinates()
        //{
        //    Random rnd = new Random();
        //    int w = pnlGraph.Width;
        //    int h = pnlGraph.Height;

        //    // SAFETY CHECK: If panel is too small or hidden, pretend it is 300x300
        //    // This PREVENTS the "minValue > maxValue" crash.
        //    int effectiveW = (w < 100) ? 300 : w;
        //    int effectiveH = (h < 100) ? 300 : h;

        //    foreach (var node in _graphManager.Nodes.Values)
        //    {
        //        // Keep nodes away from the very edge (margin of 50px)
        //        int safeMaxX = effectiveW - 60;
        //        int safeMaxY = effectiveH - 60;

        //        // Ensure we don't pass a bad number to Random
        //        int x = rnd.Next(20, Math.Max(21, safeMaxX));
        //        int y = rnd.Next(20, Math.Max(21, safeMaxY));

        //        node.Location = new Point(x, y);
        //    }
        //}

        private void pnlGraph_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Pen edgePen = new Pen(Color.Gray, 2);
            Pen cyclePen = new Pen(Color.Red, 3);
            Brush nodeBrush = Brushes.LightBlue;
            Brush completedBrush = Brushes.LightGreen;
            Brush currentBrush = Brushes.Orange;

            // Draw Edges First
            foreach (var node in _graphManager.Nodes.Values)
            {
                foreach (var neighbor in node.Neighbors)
                {
                    // Draw arrow
                    AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                    edgePen.CustomEndCap = bigArrow;
                    g.DrawLine(edgePen, node.Location, neighbor.Location);
                }
            }
            // new

            // Define fonts and sizes
            Font nodeFont = new Font("Arial", 12, FontStyle.Bold); // Bigger Text
            int nodeSize = 50; // Bigger Circle (was 30)

            // Draw Nodes
            foreach (var node in _graphManager.Nodes.Values)
            {
                Brush b = nodeBrush;

                // Color Logic for Step-by-Step
                if (_currentStepIndex >= 0)
                {
                    string activeNode = _executionOrder[_currentStepIndex];
                    if (node.Name == activeNode) b = currentBrush; // Currently processing
                    else if (_executionOrder.IndexOf(node.Name) < _currentStepIndex) b = completedBrush; // Already done
                }

                // new
                // Draw Bigger Circle
                g.FillEllipse(b, node.Location.X, node.Location.Y, nodeSize, nodeSize);
                g.DrawEllipse(Pens.Black, node.Location.X, node.Location.Y, nodeSize, nodeSize);

                // Draw Bigger Text (Centered)
                SizeF textSize = g.MeasureString(node.Name, nodeFont);
                float textX = node.Location.X + (nodeSize - textSize.Width) / 2;
                float textY = node.Location.Y - 20; // Put text ABOVE the node so it doesn't overlap

                g.DrawString(node.Name, nodeFont, Brushes.Black, textX, textY);

                // old

                //// Draw Circle
                //g.FillEllipse(b, node.Location.X - 15, node.Location.Y - 15, 30, 30);
                //g.DrawEllipse(Pens.Black, node.Location.X - 15, node.Location.Y - 15, 30, 30);

                //// Draw Text
                //g.DrawString(node.Name, this.Font, Brushes.Black, node.Location.X - 10, node.Location.Y - 5);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void lblStatus_Click(object sender, EventArgs e)
        {

        }
    }
}
