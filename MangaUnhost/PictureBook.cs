////////////////////////////////////////////////////////////////////////////
// Copyright : Robert Hartman 
// Date:       2/25/2006
// 
// Email : hart_dev@yahoo.com 
// 
// This file may be redistributed unmodified by any means PROVIDING it is
// not sold for profit without the authors written consent, and
// providing that this notice and the authors name is included.
//
// This file is provided 'as is' with no expressed or implied warranty.
// The author accepts no liability for damages caused by the use of this software.
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MangaUnhost {

    [ToolboxItem(true)]
    public partial class TurnThePageControl : UserControl {

        private Image LeftBinder;
        private Image RightBinder;
        // tickSpeed, is the timer interval in milliseconds. Controls the frame rate
        private int tickSpeed = 15;

        // step size in the X direction. Controls the number of frames
        private int MOVE_X_BY = 10;

        /* Debug constants and illustrative points*/
        // INCLUDE_DRAW_GRAPHICS_PATH will draw the outline of 
        // graphic paths during the page turn effect
        private const bool INCLUDE_DRAW_GRAPHICS_PATH = false;

        // INCLUDE_UNDERSIDE_PAGE_IN_ANIMATION will 
        // show the underside of the page being turned. 
        // Setting this value to false will not show the 
        // the underneath side of the page being animated. 
        private const bool INCLUDE_UNDERSIDE_PAGE_IN_ANIMATION = true;

        // Use PixelOffsetMode.  This sets the 
        // PixelOffsetMode to PixelOffsetMode.Half
        // of the combined bitmap. If this mode is not
        // used I simply add or subtract 1 pixel when
        // drawing areas B and C the page being turned
        private const bool USE_PIXEL_MODE_OFFSET = false;

        // INCLUDE_SPIRAL_SPINE will include the Spiral binder
        // in the animation
        private const bool INCLUDE_SPINE = true;
        // INCLUDE_DRAW_HOTSPOT will cause a small half circle around 
        // the point of page rotation.  
        private const bool INCLUDE_DRAW_HOTSPOT = false;

        private int PAGE_SPINE;   // Middle of the two pages
        private int BOOK_WIDTH;
        private int PAGE_BOTTOM;  // Y coordinate of the bottom of the page
        private int PAGE_WIDTH { get { return bitmaps[currentPage].Width; } }   // Width of the page
        private int PAGE_HEIGHT { get { return bitmaps[currentPage].Height - heightOffsetForAnimationTop; } }   // Height of the page

        //Sample Bitmaps, used to make the pages of this Picture Book
        public Bitmap[] bitmaps; // Container for all pages in this book

        // Combined offscreen image
        public Image CurrentShownBitmap {
            get { return PictureBox.Image; }
            set { PictureBox.Image = value; }
        }

        // Represents the page under the current page being turned
        private Bitmap NextUnderBitmap;

        // Represents the page on the underside of the page being turned
        private Bitmap UndersideBitmap;

        // State variable indicating that the picture book is currently
        // animating a page turn
        private bool isInAnimation;

        // Track if the current line of symmetry defines a trapezoid of a triangle.
        private bool isPathTrapezoid;

        // Start on page 3 (left page) and page 4 (right page)
        public int currentPage = 0;
        private int currentRightPage { get { return currentPage + 1; } }

        private int animatingToLeftPage;
        private int animatingToRightPage { get { return animatingToLeftPage + 1; } }

        // pathAngleInflection is the angle at which the underside
        // path is changed from a triangle to a trapezoid
        //private double pathAngleInflection;
        // X distance where the path is change
        // from a triangle to a trapezoid.
        //private int xWidthInflection;
        // Current location from the edge of the page.
        private int currentXdistanceFromEdge = 0;

        // animationTurnType indicates to algorithms if the
        // page is turning to the right or to the left
        private TurnType animationTurnType;

        // This is the location of the line of symmetry
        private Circle hotSpot;

        // This value allows the control to have room at the
        // top so the user can see the top of the pages as they
        // are turned.
        private int heightOffsetForAnimationTop;

        public TurnThePageControl() {
            InitializeComponent();
            // Get this type's assembly
            Assembly assem = this.GetType().Assembly;
            /* Get the Left and Right binders from project resources */
            Stream stream = assem.GetManifestResourceStream("MangaUnhost.Resources.SingleBinderLeft.bmp");
            // Load the bitmap from the stream
            LeftBinder = new Bitmap(stream);
            stream = assem.GetManifestResourceStream("MangaUnhost.Resources.SingleBinderRight.bmp");
            RightBinder = new Bitmap(stream);
            heightOffsetForAnimationTop = 0;
            isPathTrapezoid = false;

            // The hotSpot is the location of the line of symmetry.
            hotSpot = new Circle(new Point(230, 370), 6);
            PAGE_SPINE = ClientSize.Width / 2; // This should be the middle
            PAGE_BOTTOM = ClientSize.Height; // Location of the bottom of the pages. Height from the top of the control
            BOOK_WIDTH = ClientSize.Width; // Edge of the page
            CurrentShownBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
        }

        // Simply controls the speed of the animation
        public int TickSpeed {
            get {
                return tickSpeed;
            }
            set {
                tickSpeed = value;
            }
        }

        //Simply controls the number of times that the animation is drawn
        public int MoveXBy {
            get {
                return MOVE_X_BY;
            }
            set {
                if (value <= 1) {
                    MOVE_X_BY = 2;
                } else {
                    MOVE_X_BY = value;
                }

            }
        }
        //Gives the control a little more room so clipping of the underside page
        //does not happen.  
        public int HeightAdjustment {
            get {
                return heightOffsetForAnimationTop;
            }
            set {
                heightOffsetForAnimationTop = value;
            }
        }

        #region NOT_USED
        // This function will calculate the angle and the distance at 
        // which the page turn region changes from a triangle to a
        // trapezoid .  These values are very important in the 
        // calculation of various graphic paths.  The paths will be
        // used to control the animation of a page.
        //
        //  a= maxTriangleAngle = angle at which for a given 
        //  distance x=maxTriangleDistanceFromPE, the height of the 
        //  triangle will be the height of the bitmap.  It is assumed
        //  that when x = 0 the angle a below is 45 degrees.  As the 
        //  distance x increases the angle a will
        //  also increase by a function of x to 90 degrees.
        //   
        //     
        //           + Bitmap Height
        //         + |    
        //       +   |
        //     +     |   
        //   +       | 
        // +a____x___| 
        //  maxTriangleDistanceFromPE
        //
        // Bitmap Height - is the height of the bitmap. It lies along
        //                 the page edge.
        //
        // x - is the distance from the page edge.  As x is increased
        //     so will the calculated height for the triangle.
        //     When the calculated height is greater than or equal to
        //     the Bitmap Height, this routine will stop.  This
        //     is a non-linear equation.
        //
        //
        //  After x is greater than the calculated maxTriangleDistanceFromPE,
        //  the triangle will turn into a trapezoid for the remainder
        //  of the page turn animation.    
        //        ________  
        //       +        |
        //      +         |    
        //     +          |
        //    +           | Bitmap Height. Lies along the page edge PE
        //   +            |
        // +a____x________| 
        //  maxTriangleDistanceFromPE

        private void calcPathChangeDistAngle(float height, float width, ref double outAngle, ref int distanceFromPE) {
            double radians;
            double a;
            double tan;
            double calculatedHeight;
            double previousHeight;
            double previous_a = 0;

            // Because the Angle a is a function of the distance from the page edge   
            // and the calculated height is also a function of x; 
            // the resulting equation is a non linear equation.
            //             ------- Angle  a ---------- 
            //  h = x Tan ( 45 + ((45 * (x)) / width) )
            //
            // One way to solve this problem is using a brute force approach.  This
            // is not efficient but it solves the problem for
            // a limited and bounded value for x.
            //
            for (int x = 0; x < width; x++) {
                a = 45 + ((45 * (x)) / width);
                radians = a * (Math.PI / 180);
                tan = Math.Tan(radians);

                calculatedHeight = (x) * Math.Tan(radians);

                // These will check to see if the distance limit has been reached and also
                // save that distance and the corresponding angle.
                if (calculatedHeight >= (double)height) {
                    distanceFromPE = x;
                    outAngle = a;
                    //                    distanceFromPE = (x - 1);
                    //                    outAngle = previous_a;
                    break;
                }
                previous_a = a;
                previousHeight = calculatedHeight;


            }
        }
        #endregion
        

        public void ForceRender() {
            if (CurrentShownBitmap != null && bitmaps != null && bitmaps.Length > 2) {
                this.BackColor = this.Parent.BackColor;
                Graphics g = Graphics.FromImage(CurrentShownBitmap);
                g.Clear(this.BackColor);
                // Show page 1 and page 2. Start in the middle
                g.DrawImage(bitmaps[0], new Point(0, heightOffsetForAnimationTop));
                g.DrawImage(bitmaps[1], new Point(bitmaps[2].Width, heightOffsetForAnimationTop));
                g.Dispose();
            }
        }

        protected override void OnLoad(EventArgs e) {
            if (CurrentShownBitmap != null) {
                ForceRender();
            }
        }

        // This routine is used to get the graphics path for the page under the current page and the page on the 
        // backside of the turning page.  It will return either a triangle or trapezoid graphics path.
        private GraphicsPath GetPageUnderGraphicsPath(int x, ref double a, int height, int width, bool isUnderSide, TurnType type) {
            double radians;
            double calculated_x;
            double calculated_y = 0d;
            int undersideOffset = 0;

            GraphicsPath gp = new GraphicsPath();

            if ((type == TurnType.RightPageTurn && isUnderSide) || (type == TurnType.LeftPageTurn && !isUnderSide)) {
                undersideOffset = width;
            }

            // This is the angle formed at the bottom of the rectangle just under the line of symmetry
            a = 45d + ((45d * x) / width);
            radians = a * (Math.PI / 180); // convert to radians for the math function

            if (isPathTrapezoid == false) {
                calculated_y = (x) * (Math.Tan(radians));
                if (calculated_y > height) {
                    isPathTrapezoid = true;
                }
            }

            if (isPathTrapezoid == true) {
                gp.AddLine(new PointF(Math.Abs(width - x - undersideOffset), height), new PointF(width - undersideOffset, height));
                gp.AddLine(new PointF(width - undersideOffset, height), new PointF(width - undersideOffset, 0));



                calculated_x = height / Math.Tan(radians);
                // This adds a line to the top of the trapezoid, this is the distance in the horizontal direction
                // that the line of symmetry has traveled.
                gp.AddLine(new Point(width - undersideOffset, 0), new PointF(Math.Abs(width - (x - (float)calculated_x) - undersideOffset), 0));
                gp.CloseFigure();

            } else {
                // Still a triangle
                calculated_y = (x) * (Math.Tan(radians));
                gp.AddLine(new PointF(Math.Abs(width - x - undersideOffset), height), new PointF(width - undersideOffset, height));
                gp.AddLine(new PointF(width - undersideOffset, height), new PointF(width - undersideOffset, (height - (float)calculated_y)));

                gp.CloseFigure();

            }

            return gp;
        }
        

        protected override void OnPaintBackground(PaintEventArgs e) {
            base.OnPaintBackground(e);
        }

        // This function begins the left page turn animation.
        public void animateLeftPageTurn() {
            if (isInAnimation == false && currentPage > 1) {

                animationTurnType = TurnType.LeftPageTurn;
                currentXdistanceFromEdge = 0;
                isPathTrapezoid = false;
                isInAnimation = true;
                timer1.Interval = tickSpeed;
                timer1.Enabled = true;
                animatingToLeftPage = currentPage - 2;

                NextUnderBitmap = bitmaps[animatingToLeftPage - 1];
                UndersideBitmap = bitmaps[animatingToRightPage - 1];

                hotSpot.Origin = new Point(0, PAGE_BOTTOM);

            }


        }
        // This function begins the right page turn animation
        public void animateRightPageTurn() {
            if (isInAnimation == false && currentPage < 5) {
                animationTurnType = TurnType.RightPageTurn;
                currentXdistanceFromEdge = 0;
                isPathTrapezoid = false;
                isInAnimation = true;
                timer1.Interval = tickSpeed;
                timer1.Enabled = true;
                animatingToLeftPage = currentPage + 2;
                hotSpot.Origin = new Point(BOOK_WIDTH, PAGE_BOTTOM);
                NextUnderBitmap = bitmaps[animatingToRightPage - 1];
                UndersideBitmap = bitmaps[animatingToLeftPage - 1];
            }

        }

        // TBD.  This routine will draw a shadow  along the line of symmetry
        private void DrawShadow(Graphics g, Point start, Point end, int pageWidth) {

        }

        // Draw the page spine on a left page.  This will put the page spine on the right 
        // side of the page
        private void DrawLeftPageSpine(Graphics g, int pageMiddle, int pageWidth, int pageHeight) {
            int heightOffset = 0;
            int numBinders = 0;

            numBinders = pageHeight / LeftBinder.Height;

            heightOffset = (pageHeight - (numBinders * LeftBinder.Height)) / 2;

            g.TranslateTransform(pageMiddle, 0);
            for (int i = 0; i < numBinders; i++) {
                g.DrawImage(LeftBinder, new Point(-LeftBinder.Width, heightOffset + (i * LeftBinder.Height)));
            }
            g.ResetTransform();

        }

        // Draw the page spine on a right page.  This will put the page spine on the left 
        // side of the page
        private void DrawRightPageSpine(Graphics g, int pageMiddle, int pageWidth, int pageHeight) {
            int heightOffset = 0;
            int numBinders = 0;

            numBinders = pageHeight / LeftBinder.Height;

            heightOffset = (pageHeight - (numBinders * LeftBinder.Height)) / 2;

            for (int i = 0; i < numBinders; i++) {
                g.DrawImage(RightBinder, new Point(0, heightOffset + (i * LeftBinder.Height)));
            }

        }

        // This is the main routine for the animation.
        // When the timer ticks the page turn effect will be 
        // advanced. 
        private void timer1_Tick(object sender, EventArgs e) {
            int timeStart = Environment.TickCount;
            int timeStop;
            double pageUnderAngle;
            double pageUndersideRotationAngle;
            double radians;
            double angle_a = 0;


            GraphicsPath pageUnderGraphicsPath;
            RectangleF undersidePathBounds;
            Bitmap pageUndersideImage = null;
            Graphics g; // Graphics to the combined image
            Graphics undersideG; // Graphics to the underside image, corresponds to pageUndersideImage
            PixelOffsetMode p;

            Region oldClipRegion;
            Matrix oldTransform;
            Matrix PathTranslationMatrix;

            // Get a graphics context for the combined image that will show the current pages
            // and the animation effects
            g = Graphics.FromImage(CurrentShownBitmap);

            g.Clear(this.BackColor);

            // get a new distance from the edge of the page (either left or right). This is the 
            // distance along the bottom of the page.
            currentXdistanceFromEdge += MOVE_X_BY;

            if (currentXdistanceFromEdge >= (PAGE_SPINE)) {
                timer1.Enabled = false;
                isInAnimation = false;

                currentPage = animatingToLeftPage;


                // At this point the animation is done, draw in the new bitmaps fully
                g.DrawImage(bitmaps[currentPage - 1], new Point(0, heightOffsetForAnimationTop));
                g.DrawImage(bitmaps[currentRightPage - 1], new Point(bitmaps[currentPage - 1].Width, heightOffsetForAnimationTop));


            } else {
                // An animation is in progress, draw in the current bitmaps. This serves as the base images.
                // All animiation effects will be drawn on top of this.
                g.DrawImage(bitmaps[currentPage + 1], new Point(0, heightOffsetForAnimationTop));
                g.DrawImage(bitmaps[currentRightPage + 1], new Point(bitmaps[currentPage + 1].Width, heightOffsetForAnimationTop));


                // During rotation the the underside of the page being turned needs to be displayed. To display this
                // image a copy of it is needed to control clipping and then eventual rotation along the line of symmetery.
                pageUndersideImage = new Bitmap(PAGE_WIDTH, PAGE_HEIGHT);

                if (animationTurnType == TurnType.LeftPageTurn) {
                    #region LEFTTURN
                    // Set the current point of the page turn
                    hotSpot.translateOrigin(MOVE_X_BY, 0);

                    // Get the new graphics path for the underlying page
                    pageUnderGraphicsPath = GetPageUnderGraphicsPath(currentXdistanceFromEdge, ref angle_a, PAGE_HEIGHT, PAGE_WIDTH, false, animationTurnType);
                    pageUndersideRotationAngle = -(180d - (2 * angle_a));

                    PathTranslationMatrix = new Matrix();
                    PathTranslationMatrix.Translate((float)0, (float)heightOffsetForAnimationTop);
                    pageUnderGraphicsPath.Transform(PathTranslationMatrix);

                    //Save the old clip region
                    oldClipRegion = g.Clip;
                    // Set the new clip region to be that of the calcualted path.
                    g.Clip = new Region(pageUnderGraphicsPath);
                    g.DrawImage(NextUnderBitmap, new Point(0, heightOffsetForAnimationTop));
                    g.Clip = oldClipRegion;
                    if (INCLUDE_DRAW_GRAPHICS_PATH == true) {
                        g.DrawPath(new Pen(Brushes.Gold, 5), pageUnderGraphicsPath);
                    }
                    PathTranslationMatrix.Dispose();
                    // Now Draw the graphics for the page underside of the page being rotated.


                    // In this section the back side of the current page will be drawn
                    // This path tranformation will reverse the x coordinates.
                    pageUnderGraphicsPath = GetPageUnderGraphicsPath(currentXdistanceFromEdge, ref angle_a, PAGE_HEIGHT, PAGE_WIDTH, true, animationTurnType);
                    undersidePathBounds = pageUnderGraphicsPath.GetBounds();

                    //pageUndersideImage

                    if (undersidePathBounds.Width > 0) {
                        // Get the graphics object for the underside of the image being turned
                        undersideG = Graphics.FromImage(pageUndersideImage);
                        if (USE_PIXEL_MODE_OFFSET == true) {
                            undersideG.PixelOffsetMode = PixelOffsetMode.Half;
                        }

                        // Set the clip region to the underside of the currently being turned image
                        undersideG.Clip = new Region(pageUnderGraphicsPath);
                        // Draw the image; note, only the clip region should be shown
                        undersideG.DrawImage(UndersideBitmap, new Point(0, 0));
                        // Dispose of the graphics object
                        undersideG.Dispose();

                        // Now this image needs to be translated to the hotspot and then rotated.
                        PathTranslationMatrix = new Matrix(); // setup the translation matrix
                        if (USE_PIXEL_MODE_OFFSET == true) {
                            PathTranslationMatrix.Translate((float)hotSpot.Origin.X, (float)hotSpot.Origin.Y);
                        } else {
                            PathTranslationMatrix.Translate((float)hotSpot.Origin.X - 1, (float)hotSpot.Origin.Y);
                        }

                        PathTranslationMatrix.Rotate((float)((pageUndersideRotationAngle)));
                        oldTransform = g.Transform;
                        g.Transform = PathTranslationMatrix;

                        if (INCLUDE_UNDERSIDE_PAGE_IN_ANIMATION == true) {//
                            //g.DrawImage(pageUndersideImage, currentXdistanceFromEdge, -PAGE_HEIGHT);
                            g.DrawImage(pageUndersideImage, hotSpot.Origin.X - PAGE_WIDTH, -PAGE_HEIGHT);//-UndersideBitmap.Height);
                        }
                        g.Transform = oldTransform;
                    }

                    #endregion

                } else // The right page is turning
                  {
                    #region RIGHTTURN
                    // Set the current point of the page turn
                    hotSpot.translateOrigin(-MOVE_X_BY, 0);

                    // Get the new graphics path for the underlying page
                    pageUnderGraphicsPath = GetPageUnderGraphicsPath(currentXdistanceFromEdge, ref angle_a, PAGE_HEIGHT, PAGE_WIDTH, false, animationTurnType);
                    pageUndersideRotationAngle = (180d - (2 * angle_a));

                    PathTranslationMatrix = new Matrix();
                    PathTranslationMatrix.Translate((float)PAGE_SPINE, (float)heightOffsetForAnimationTop);
                    pageUnderGraphicsPath.Transform(PathTranslationMatrix);

                    //Save the old clip region
                    oldClipRegion = g.Clip;

                    // Setting the PixelOffsetMode will start drawing -.5f .
                    // The pixel offset mode is needed on this graphics object
                    // to draw the page under area.
                    if (USE_PIXEL_MODE_OFFSET == true) {
                        p = g.PixelOffsetMode;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                    }

                    // Set the new clip region to be that of the calcualted path.
                    g.Clip = new Region(pageUnderGraphicsPath);
                    g.DrawImage(NextUnderBitmap, new Point(PAGE_SPINE, heightOffsetForAnimationTop));
                    g.Clip = oldClipRegion;
                    if (INCLUDE_DRAW_GRAPHICS_PATH == true) {
                        g.DrawPath(new Pen(Brushes.Gold, 5), pageUnderGraphicsPath);
                    }
                    if (USE_PIXEL_MODE_OFFSET == true) {
                        // Restore the pixel offset mode
                        g.PixelOffsetMode = p;
                    }

                    PathTranslationMatrix.Dispose();
                    // Now Draw the graphics for the page underside of the page being rotated.

                    // In this section the back side of the current page will be drawn
                    // This path tranformation will reverse the x coordinates.
                    pageUnderGraphicsPath = GetPageUnderGraphicsPath(currentXdistanceFromEdge, ref angle_a, PAGE_HEIGHT, PAGE_WIDTH, true, animationTurnType);
                    undersidePathBounds = pageUnderGraphicsPath.GetBounds();

                    if (undersidePathBounds.Width > 0) {
                        // Get the graphics object for the underside of the image being turned
                        undersideG = Graphics.FromImage(pageUndersideImage);

                        // Set the clip region to the underside of the currently being turned image
                        undersideG.Clip = new Region(pageUnderGraphicsPath);

                        // Draw the image; note, only the clip region should be shown
                        undersideG.DrawImage(UndersideBitmap, new Point(0, 0));
                        // Dispose of the graphics object
                        undersideG.Dispose();

                        // Now this image needs to be translated to the hotspot and then rotated.
                        PathTranslationMatrix = new Matrix(); // setup the translation matrix
                        if (USE_PIXEL_MODE_OFFSET == true) {
                            PathTranslationMatrix.Translate((float)hotSpot.Origin.X, (float)hotSpot.Origin.Y);
                        } else // Simulate a pixel offset by adding 1. This will cause the underside image
                               // to slightly overlap the page under area currently displayed.
                          {
                            PathTranslationMatrix.Translate((float)hotSpot.Origin.X + 1, (float)hotSpot.Origin.Y);
                        }
                        PathTranslationMatrix.Rotate((float)((pageUndersideRotationAngle)));


                        oldTransform = g.Transform;
                        g.Transform = PathTranslationMatrix;
                        if (INCLUDE_UNDERSIDE_PAGE_IN_ANIMATION == true) {
                            g.DrawImage(pageUndersideImage, -(currentXdistanceFromEdge), -(PAGE_HEIGHT));
                        }
                        g.Transform = oldTransform;


                    }
                    #endregion
                }
                if (INCLUDE_DRAW_HOTSPOT == true) {
                    hotSpot.Draw(g, Color.Snow);
                }
                timeStop = Environment.TickCount;
                int test = timeStop - timeStart;

            }

            if (pageUndersideImage != null) {
                pageUndersideImage.Dispose();
            }
            g.Dispose();
            PictureBox.Invalidate();
            Invalidate();
        }

        private void Resized(object sender, EventArgs e) {
            CurrentShownBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            // The hotSpot is the location of the line of symmetry.
            hotSpot = new Circle(new Point(230, 370), 6);
            PAGE_SPINE = ClientSize.Width / 2; // This should be the middle
            PAGE_BOTTOM = ClientSize.Height; // Location of the bottom of the pages. Height from the top of the control
            BOOK_WIDTH = ClientSize.Width; // Edge of the page
            ForceRender();
        }

        private void Clicked(object sender, MouseEventArgs e) {
            if (e.Location.X < PictureBox.Width / 2)
                animateLeftPageTurn();
            else
                animateRightPageTurn();
        }
    }

    // Indicates if the Left page or the right
    // page is being turned for the current 
    // animation
    enum TurnType {
        LeftPageTurn,
        RightPageTurn
    }

    enum TurnEffect {
        Angled,
        Vertical //not implemented
    }

    // Simple class used to represent the
    // location of the line of symmetry
    public class Circle {
        public Point origin;
        public int radius;

        public Circle(Point _origin, int _radius) {
            origin = _origin;
            radius = _radius;

            //origin = new Point(50, 200);

        }

        public Point Origin {
            get {
                return origin;
            }
            set {

                origin = value;


            }


        }

        public void translateOrigin(int X, int Y) {
            origin.X = Origin.X + X;
            origin.Y = Origin.Y + Y;
        }


        public bool hitTest(Point p) {
            if (p.X == origin.X && p.Y == origin.Y) {
                return true;
            } else {
                if (p.X > (origin.X - radius / 2) && p.X < (origin.X + radius / 2) && (p.Y > origin.Y - radius / 2) && (p.Y < origin.Y + radius / 2)) {

                    return true;

                }

            }
            return false;

        }

        public void Draw(Graphics g, Color c) {
            SolidBrush b = new SolidBrush(c);
            g.DrawEllipse(new Pen(b), origin.X - radius / 2, origin.Y - radius / 2, radius, radius);

        }
    }

}