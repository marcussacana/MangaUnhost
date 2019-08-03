using CefSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MangaUnhost.Others
{
    public static class CursorTools {

        public static List<MimicStep> CreateMove(Point From, Point Target, int MouseSpeed = 8) => CreateMove(From.X, From.Y, Target.X, Target.Y, MouseSpeed);
        public static List<MimicStep> CreateMove(int FromX, int FromY, int TargetX, int TargetY, int MouseSpeed = 8) {
            int rx = 10, ry = 10;

            Random random = new Random();

            TargetX += random.Next(rx);
            TargetY += random.Next(ry);

            double randomSpeed = Math.Max((random.Next(MouseSpeed) / 2.0 + MouseSpeed) / 10.0, 0.1);

            return WindMouse(FromX, FromY, TargetX, TargetY, 10.0, 5.0, 10.0 / randomSpeed, 15.0 / randomSpeed, 10.0 * randomSpeed, 10.0 * randomSpeed);
        }

        static List<MimicStep> WindMouse(double xs, double ys, double xe, double ye,
            double gravity, double wind, double minWait, double maxWait,
            double maxStep, double targetArea) {

            double dist, windX = 0, windY = 0, veloX = 0, veloY = 0, randomDist, veloMag, step;
            int oldX, oldY, newX = (int)Math.Round(xs), newY = (int)Math.Round(ys);

            double waitDiff = maxWait - minWait;
            double sqrt2 = Math.Sqrt(2.0);
            double sqrt3 = Math.Sqrt(3.0);
            double sqrt5 = Math.Sqrt(5.0);

            var Steps = new List<MimicStep>();
            Random random = new Random();

            dist = Hypot(xe - xs, ye - ys);

            while (dist > 1.0) {

                wind = Math.Min(wind, dist);

                if (dist >= targetArea) {
                    int w = random.Next((int)Math.Round(wind) * 2 + 1);
                    windX = windX / sqrt3 + (w - wind) / sqrt5;
                    windY = windY / sqrt3 + (w - wind) / sqrt5;
                } else {
                    windX = windX / sqrt2;
                    windY = windY / sqrt2;
                    if (maxStep < 3)
                        maxStep = random.Next(3) + 3.0;
                    else
                        maxStep = maxStep / sqrt5;
                }

                veloX += windX;
                veloY += windY;
                veloX = veloX + gravity * (xe - xs) / dist;
                veloY = veloY + gravity * (ye - ys) / dist;

                if (Hypot(veloX, veloY) > maxStep) {
                    randomDist = maxStep / 2.0 + random.Next((int)Math.Round(maxStep) / 2);
                    veloMag = Hypot(veloX, veloY);
                    veloX = (veloX / veloMag) * randomDist;
                    veloY = (veloY / veloMag) * randomDist;
                }

                oldX = (int)Math.Round(xs);
                oldY = (int)Math.Round(ys);
                xs += veloX;
                ys += veloY;
                dist = Hypot(xe - xs, ye - ys);
                newX = (int)Math.Round(xs);
                newY = (int)Math.Round(ys);


                step = Hypot(xs - oldX, ys - oldY);
                int wait = (int)Math.Round(waitDiff * (step / maxStep) + minWait);

                if (oldX != newX || oldY != newY || wait != 0)
                    Steps.Add(new MimicStep(newX, newY, wait));
            }

            int endX = (int)Math.Round(xe);
            int endY = (int)Math.Round(ye);
            if (endX != newX || endY != newY)
                Steps.Add(new MimicStep(endX, endY, random.Next(1500)));

            return Steps;
        }

        static double Hypot(double dx, double dy) {
            return Math.Sqrt(dx * dx + dy * dy);
        }

    }

	public struct MimicStep {
        public Point Location;
        public int Delay;

		public MimicStep(int X, int Y, int Delay) {
            Location = new Point(X, Y);
            this.Delay = Delay;
        }
    }
}
