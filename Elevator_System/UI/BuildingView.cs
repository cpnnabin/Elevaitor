using System.Drawing.Drawing2D;
using Lift.Models;
using Lift.Subject;

namespace Lift.UI
{
    /// <summary>Draws the two shafts, the cars, the doors and the door-sensor object. It only READS lift data.</summary>
    public sealed class BuildingView : Control
    {
        private const int Pad = 10;
        private const int SlabHeight = 8;
        private const int DisplayStripHeight = 24;

        private readonly IReadOnlyList<Elevator> _lifts;

        public BuildingView(IReadOnlyList<Elevator> lifts)
        {
            _lifts = lifts;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(235, 235, 235));
            if (_lifts.Count == 0) return;

            int floorCount = FloorInfo.MaxFloor - FloorInfo.MinFloor + 1;
            int floorH = (Height - 2 * Pad) / floorCount;
            int shaftW = 170, gap = 40;
            int firstShaftX = (Width - (_lifts.Count * shaftW + (_lifts.Count - 1) * gap)) / 2;

            using var slabBrush = new SolidBrush(Color.FromArgb(90, 90, 90));
            using var shaftBrush = new SolidBrush(Color.FromArgb(55, 55, 60));
            using var captionFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var liftFont = new Font("Consolas", 11f, FontStyle.Bold);

            // shafts
            for (int i = 0; i < _lifts.Count; i++)
                g.FillRectangle(shaftBrush, firstShaftX + i * (shaftW + gap), Pad, shaftW, floorCount * floorH);

            // floor slabs and captions
            for (int floor = FloorInfo.MinFloor; floor <= FloorInfo.MaxFloor; floor++)
            {
                int top = Pad + (FloorInfo.MaxFloor - floor) * floorH;
                g.FillRectangle(slabBrush, 0, top + floorH - SlabHeight, Width, SlabHeight);
                g.DrawString(FloorInfo.Name(floor).ToUpper(), captionFont, Brushes.DimGray, 6, top + floorH - SlabHeight - 20);
            }

            // cars
            for (int i = 0; i < _lifts.Count; i++)
                DrawCar(g, _lifts[i], firstShaftX + i * (shaftW + gap), shaftW, floorH, liftFont);
        }

        private static void DrawCar(Graphics g, Elevator lift, int shaftX, int shaftW, int floorH, Font font)
        {
            int carW = shaftW - 30;
            int carH = floorH - SlabHeight - 30;
            int carX = shaftX + 15;
            int floorCount = FloorInfo.MaxFloor - FloorInfo.MinFloor + 1;

            int rise = (int)Math.Round((lift.Position - FloorInfo.MinFloor) * floorH);
            int carBottom = Pad + floorCount * floorH - SlabHeight - rise;
            int carTop = carBottom - carH;

            // inside of the car
            using (var interior = new SolidBrush(Color.FromArgb(225, 225, 230)))
                g.FillRectangle(interior, carX, carTop, carW, carH);

            // doors: two leaves that slide to the sides
            int doorTop = carTop + DisplayStripHeight;
            int doorH = carH - DisplayStripHeight;
            int half = carW / 2;
            int leaf = (int)Math.Round(half * (1.0 - 0.9 * lift.DoorOpenness));
            using (var doorBrush = new SolidBrush(Color.Silver))
            using (var doorEdge = new Pen(Color.DimGray))
            {
                g.FillRectangle(doorBrush, carX, doorTop, leaf, doorH);
                g.FillRectangle(doorBrush, carX + carW - leaf, doorTop, leaf, doorH);
                g.DrawRectangle(doorEdge, carX, doorTop, leaf, doorH);
                g.DrawRectangle(doorEdge, carX + carW - leaf, doorTop, leaf, doorH);
            }

            // object in the doorway (door sensor)
            if (lift.ObstructionDetected)
            {
                using var objectBrush = new SolidBrush(Color.DarkOrange);
                g.FillRectangle(objectBrush, carX + carW / 2 - 12, carBottom - 50, 24, 50);
                g.DrawString("!", font, Brushes.White, carX + carW / 2 - 5, carBottom - 34);
            }

            // display strip on top of the car
            g.FillRectangle(Brushes.Black, carX, carTop, carW, DisplayStripHeight);
            string text = $"{lift.Name}  {FloorInfo.Label(lift.NearestFloor)} {lift.Direction.Arrow()}";
            g.DrawString(text, font, Brushes.Red, carX + 6, carTop + 3);

            // outline (red when the SOS is on)
            using var outline = new Pen(lift.EmergencyActive ? Color.Red : Color.Black, lift.EmergencyActive ? 4 : 2);
            g.DrawRectangle(outline, carX, carTop, carW, carH);
        }
    }
}
