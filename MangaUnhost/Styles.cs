using System;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;


#region Global Functions/ Subs/ Enums
internal static class DrawHelpers {
    public static GraphicsPath RoundRectangle(Rectangle Rectangle, int Curve) {
        GraphicsPath P = new GraphicsPath();
        int ArcRectangleWidth = Curve * 2;
        P.AddArc(new Rectangle(Rectangle.X, Rectangle.Y, ArcRectangleWidth, ArcRectangleWidth), -180F, 90F);
        P.AddArc(new Rectangle(Rectangle.Width - ArcRectangleWidth + Rectangle.X, Rectangle.Y, ArcRectangleWidth, ArcRectangleWidth), -90F, 90F);
        P.AddArc(new Rectangle(Rectangle.Width - ArcRectangleWidth + Rectangle.X, Rectangle.Height - ArcRectangleWidth + Rectangle.Y, ArcRectangleWidth, ArcRectangleWidth), 0F, 90F);
        P.AddArc(new Rectangle(Rectangle.X, Rectangle.Height - ArcRectangleWidth + Rectangle.Y, ArcRectangleWidth, ArcRectangleWidth), 90F, 90F);
        P.AddLine(new Point(Rectangle.X, Rectangle.Height - ArcRectangleWidth + Rectangle.Y), new Point(Rectangle.X, Curve + Rectangle.Y));
        return P;
    }

    public static GraphicsPath RoundRect(float x, float y, float w, float h, float r = 0.3F, bool TL = true, bool TR = true, bool BR = true, bool BL = true) {
        GraphicsPath tempRoundRect = null;
        float d = Math.Min(w, h) * r;
        var xw = x + w;
        var yh = y + h;
        tempRoundRect = new GraphicsPath();
        if (TL) {
            tempRoundRect.AddArc(x, y, d, d, 180, 90);
        } else {
            tempRoundRect.AddLine(x, y, x, y);
        }
        if (TR) {
            tempRoundRect.AddArc(xw - d, y, d, d, 270, 90);
        } else {
            tempRoundRect.AddLine(xw, y, xw, y);
        }
        if (BR) {
            tempRoundRect.AddArc(xw - d, yh - d, d, d, 0, 90);
        } else {
            tempRoundRect.AddLine(xw, yh, xw, yh);
        }
        if (BL) {
            tempRoundRect.AddArc(x, yh - d, d, d, 90, 90);
        } else {
            tempRoundRect.AddLine(x, yh, x, yh);
        }
        tempRoundRect.CloseFigure();
        return tempRoundRect;
    }
}
internal enum MouseState : byte {
    None = 0,
    Over = 1,
    Down = 2,
    Block = 3
}
#endregion


public class VSContainer : ContainerControl {
    private bool InstanceFieldsInitialized = false;

    private void InitializeInstanceFields() {
        _Form = Form.ActiveForm;
    }

    #region Declarations
    private bool _AllowClose = true;
    private bool _AllowMinimize = true;
    private bool _AllowMaximize = true;
    private int _FontSize = 12;
    private bool _ShowIcon = true;
    private MouseState State = MouseState.None;
    private int MouseXLoc;
    private int MouseYLoc;
    private bool CaptureMovement = false;
    private const int MoveHeight = 35;
    private Point MouseP = new Point(0, 0);
    private Color _FontColour = Color.FromArgb(153, 153, 153);
    private Color _BaseColour = Color.FromArgb(45, 45, 48);
    private Color _IconColour = Color.FromArgb(255, 255, 255);
    private Color _ControlBoxColours = Color.FromArgb(248, 248, 248);
    private Color _BorderColour = Color.FromArgb(15, 15, 18);
    private Color _HoverColour = Color.FromArgb(63, 63, 65);
    private Color _PressedColour = Color.FromArgb(0, 122, 204);
    private Font _Font = new Font("Microsoft Sans Serif", 9);
    public enum __IconStyle {
        VSIcon,
        FormIcon
    }
    private __IconStyle _IconStyle = __IconStyle.FormIcon;
    public enum __FormOrWhole {
        WholeApplication,
        Form
    }
    private __FormOrWhole _FormOrWhole = __FormOrWhole.WholeApplication;
    private Form _Form;
    #endregion

    #region Properties

    [Category("Control")]
    public __FormOrWhole FormOrWhole {
        get {
            return _FormOrWhole;
        }
        set {
            _FormOrWhole = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public Form Form {
        get {
            return _Form;
        }
        set {
            if (value == null) {
                return;
            } else {
                _Form = value;
            }

            Invalidate();
        }
    }

    [Category("Control")]
    public __IconStyle IconStyle {
        get {
            return _IconStyle;
        }
        set {
            _IconStyle = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public int FontSize {
        get {
            return _FontSize;
        }
        set {
            _FontSize = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool AllowMinimize {
        get {
            return _AllowMinimize;
        }
        set {
            _AllowMinimize = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool AllowMaximize {
        get {
            return _AllowMaximize;
        }
        set {
            _AllowMaximize = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowIcon {
        get {
            return _ShowIcon;
        }
        set {
            _ShowIcon = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool AllowClose {
        get {
            return _AllowClose;
        }
        set {
            _AllowClose = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color HoverColour {
        get {
            return _HoverColour;
        }
        set {
            _HoverColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color FontColour {
        get {
            return _FontColour;
        }
        set {
            _FontColour = value;
            Invalidate();
        }
    }

    protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e) {
        base.OnMouseUp(e);
        CaptureMovement = false;
        State = MouseState.Over;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) {
        base.OnMouseEnter(e);
        State = MouseState.Over;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        base.OnMouseLeave(e);
        State = MouseState.None;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        MouseXLoc = e.Location.X;
        MouseYLoc = e.Location.Y;
        Invalidate();
        if (CaptureMovement) {
            Parent.Location = MousePosition - (Size)MouseP;
        }
        if (e.Y > 26) {
            Cursor = Cursors.Arrow;
        } else {
            Cursor = Cursors.Hand;
        }
    }

    private delegate void _InvokeForm(MouseEventArgs e);

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        if (MouseXLoc > Width - 30 && MouseXLoc < Width && MouseYLoc < 26) {
            if (_AllowClose) {
                if (_FormOrWhole == __FormOrWhole.Form) {
                    if (_Form == null) {
                        Environment.Exit(0);
                    } else {
                        if (_Form.InvokeRequired) {
                            _Form.Invoke(new _InvokeForm(OnMouseDown), e);
                        } else {
                            _Form.Close();
                        }
                    }

                } else {
                    Environment.Exit(0);
                }
            }
        } else if (MouseXLoc > Width - 60 && MouseXLoc < Width - 30 && MouseYLoc < 26) {
            if (_AllowMaximize) {
                switch (FindForm().WindowState) {
                    case FormWindowState.Maximized:
                        FindForm().WindowState = FormWindowState.Normal;
                        break;
                    case FormWindowState.Normal:
                        FindForm().WindowState = FormWindowState.Maximized;
                        break;
                }
            }
        } else if (MouseXLoc > Width - 90 && MouseXLoc < Width - 60 && MouseYLoc < 26) {
            if (_AllowMinimize) {
                switch (FindForm().WindowState) {
                    case FormWindowState.Normal:
                        FindForm().WindowState = FormWindowState.Minimized;
                        break;
                    case FormWindowState.Maximized:
                        FindForm().WindowState = FormWindowState.Minimized;
                        break;
                }
            }
        } else if (e.Button == System.Windows.Forms.MouseButtons.Left && (new Rectangle(0, 0, Width - 90, MoveHeight)).Contains(e.Location)) {
            CaptureMovement = true;
            MouseP = e.Location;
        } else if (e.Button == System.Windows.Forms.MouseButtons.Left && (new Rectangle(Width - 90, 22, 75, 13)).Contains(e.Location)) {
            CaptureMovement = true;
            MouseP = e.Location;
        } else if (e.Button == System.Windows.Forms.MouseButtons.Left && (new Rectangle(Width - 15, 0, 15, MoveHeight)).Contains(e.Location)) {
            CaptureMovement = true;
            MouseP = e.Location;
        } else {
            Focus();
        }
        State = MouseState.Down;
        Invalidate();
    }
    #endregion

    #region Draw Control

    public VSContainer() {
        if (!InstanceFieldsInitialized) {
            InitializeInstanceFields();
            InstanceFieldsInitialized = true;
        }
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        this.DoubleBuffered = true;
        this.BackColor = _BaseColour;
        this.Dock = DockStyle.Fill;
    }

    protected override void OnCreateControl() {
        base.OnCreateControl();
        ParentForm.FormBorderStyle = FormBorderStyle.None;
        ParentForm.AllowTransparency = false;
        ParentForm.TransparencyKey = Color.Fuchsia;
        ParentForm.FindForm().StartPosition = FormStartPosition.CenterParent;
        Dock = DockStyle.Fill;
        Invalidate();
    }

    private Point[] Points1 = {
        new Point(9, 11),
        new Point(16, 17)
    };
    private Point[] Points2 = {
        new Point(9, 22),
        new Point(16, 17)
    };
    private Point[] Points3 = {
        new Point(16, 17),
        new Point(26, 7)
    };
    private Point[] Points4 = {
        new Point(16, 17),
        new Point(25, 26)
    };

    protected override void OnPaint(PaintEventArgs e) {
         if  (!(FindForm().MaximumSize == Screen.PrimaryScreen.WorkingArea.Size))
            FindForm().MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
        var G = e.Graphics;
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        G.SmoothingMode = SmoothingMode.AntiAlias;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        G.FillRectangle(new SolidBrush(_BaseColour), new Rectangle(0, 0, Width, Height));
        G.DrawRectangle(new Pen(_BorderColour), new Rectangle(0, 0, Width, Height));
        switch (State) {
            case MouseState.Over:
                if (_AllowClose && MouseXLoc > Width - 30 && MouseXLoc < Width && MouseYLoc < 26) {
                    G.FillRectangle(new SolidBrush(_HoverColour), new Rectangle(Width - 30, 1, 29, 25));
                } else if (_AllowMaximize && MouseXLoc > Width - 60 && MouseXLoc < Width - 30 && MouseYLoc < 26) {
                    G.FillRectangle(new SolidBrush(_HoverColour), new Rectangle(Width - 60, 1, 30, 25));
                } else if (_AllowMinimize && MouseXLoc > Width - 90 && MouseXLoc < Width - 60 && MouseYLoc < 26) {
                    G.FillRectangle(new SolidBrush(_HoverColour), new Rectangle(Width - 90, 1, 30, 25));
                }
                break;
        }

        //'Close Button
        if (_AllowClose) {
            G.DrawLine(new Pen(_ControlBoxColours, 2F), Width - 20, 10, Width - 12, 18);
            G.DrawLine(new Pen(_ControlBoxColours, 2F), Width - 20, 18, Width - 12, 10);
        }

        //'Minimize Button
        if (_AllowMinimize) {
            G.FillRectangle(new SolidBrush(_ControlBoxColours), Width - 79, 17, 8, 2);
        }

        //'Maximize Button
        if (_AllowMaximize) {
            if (FindForm().WindowState == FormWindowState.Normal) {
                G.DrawString("1", new Font("Webdings", 9, FontStyle.Bold), new SolidBrush(_ControlBoxColours), new RectangleF(new Point(Width - 51, 5), new Size(20, 20)));
            } else if (FindForm().WindowState == FormWindowState.Maximized) {
                G.DrawString("2", new Font("Webdings", 9, FontStyle.Bold), new SolidBrush(_ControlBoxColours), new RectangleF(new Point(Width - 51, 5), new Size(20, 20)));
            }
        }

        if (_ShowIcon) {
            switch (_IconStyle) {
                case __IconStyle.FormIcon:
                    //G.DrawIcon(FindForm().Icon, new Rectangle(6, 6, 22, 22));
                    G.DrawImage(MangaUnhost.Properties.Resources.Book, 6, 6, 22, 22);
                    G.DrawString(Text, _Font, new SolidBrush(_FontColour), new RectangleF(37F, 0F, Width - 110, 32F), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
                    break;
                default:
                    G.DrawLines(new Pen(_IconColour, 3F), Points1);
                    G.DrawLines(new Pen(_IconColour, 3F), Points2);
                    G.DrawLines(new Pen(_IconColour, 4F), Points3);
                    G.DrawLines(new Pen(_IconColour, 4F), Points4);
                    G.DrawLine(new Pen(_IconColour, 3F), new Point(9, 11), new Point(9, 22));
                    G.DrawLine(new Pen(_IconColour, 4F), 26, 6, 26, 28);
                    G.DrawString(Text, _Font, new SolidBrush(_FontColour), new RectangleF(37F, 0F, Width - 110, 32F), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
                    break;
            }
        } else {
            G.DrawString(Text, _Font, new SolidBrush(_FontColour), new RectangleF(5F, 0F, Width - 110, 30F), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
        }
        G.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

public class VSButton : Control {
    #region Declarations
    private MouseState State = MouseState.None;
    private Color _FontColour = Color.FromArgb(153, 153, 153);
    private Color _BaseColour = Color.FromArgb(45, 45, 48);
    private Color _IconColour = Color.FromArgb(255, 255, 255);
    private Color _BorderColour = Color.FromArgb(15, 15, 18);
    private Color _HoverColour = Color.FromArgb(60, 60, 62);
    private Color _PressedColour = Color.FromArgb(37, 37, 39);
    private bool _ShowBorder = true;
    private bool _ShowImage = false;
    private bool _ShowText = true;
    private Image _Image = null;
    private StringAlignment _TextAlignment = StringAlignment.Center;
    private __ImageAlignment _ImageAlignment = __ImageAlignment.Left;
    #endregion

    #region Properties

    public enum __ImageAlignment {
        Left,
        Middle,
        Right
    }

    [Category("Control")]
    public __ImageAlignment ImageAlignment {
        get {
            return _ImageAlignment;
        }
        set {
            if (_ShowText && (value == __ImageAlignment.Middle)) {
                _ImageAlignment = __ImageAlignment.Left;
            } else {
                _ImageAlignment = value;
            }
            Invalidate();
        }
    }

    [Category("Control")]
    public Image ImageChoice {
        get {
            return _Image;
        }
        set {
            _Image = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public StringAlignment TextAlignment {
        get {
            return _TextAlignment;
        }
        set {
            _TextAlignment = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowImage {
        get {
            return _ShowImage;
        }
        set {
            _ShowImage = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowText {
        get {
            return _ShowText;
        }
        set {
            if ((_ImageAlignment == __ImageAlignment.Middle) && (ShowImage == true) && (value == true)) {
                _ImageAlignment = __ImageAlignment.Left;
            }
            _ShowText = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowBorder {
        get {
            return _ShowBorder;
        }
        set {
            _ShowBorder = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    [Category("Colours")]
    public Color HoverColour {
        get {
            return _HoverColour;
        }
        set {
            _HoverColour = value;
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color FontColour {
        get {
            return _FontColour;
        }
        set {
            _FontColour = value;
        }
    }

    public object Indentifier;

    protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e) {
        base.OnMouseUp(e);
        State = MouseState.Over;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) {
        base.OnMouseEnter(e);
        State = MouseState.Over;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        base.OnMouseLeave(e);
        State = MouseState.None;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        State = MouseState.Down;
        Invalidate();
    }

    #endregion

    #region Draw Control

    public VSButton() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        this.DoubleBuffered = true;
        this.BackColor = _BaseColour;
    }

    protected override void OnPaint(PaintEventArgs e) {
        var G = e.Graphics;
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        G.SmoothingMode = SmoothingMode.AntiAlias;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        switch (State) {
            case MouseState.None:
                G.FillRectangle(new SolidBrush(_BaseColour), new Rectangle(0, 0, Width, Height));
                break;
            case MouseState.Over:
                G.FillRectangle(new SolidBrush(_HoverColour), new Rectangle(0, 0, Width, Height));
                break;
            case MouseState.Down:
                G.FillRectangle(new SolidBrush(_PressedColour), new Rectangle(0, 0, Width, Height));
                break;
        }
        if (_ShowBorder) {
            G.DrawRectangle(new Pen(_BorderColour, 1F), new Rectangle(0, 0, Width, Height));
        }
        if (_ShowImage) {
            if (_ShowText) {
                if ((Width > 50) && (Height > 30)) {
                    if (_ImageAlignment == __ImageAlignment.Left) {
                        G.DrawImage(_Image, new Rectangle(10, 10, Height - 20, Height - 20));
                        G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(0 + (Height - 5), 0, (Width - 20) - (Height - 10), Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                    } else if (_ImageAlignment == __ImageAlignment.Right) {
                        G.DrawImage(_Image, new Rectangle((Width - 20) - (Height - 20), 10, Height - 20, Height - 20));
                        G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(10, 0, (Width - 20) - (Height - 20), Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                    }
                } else {
                    G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(10, 0, Width - 20, Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                }
            } else {
                if (_ImageAlignment == __ImageAlignment.Left) {
                    G.DrawImage(_Image, new Rectangle(10, 10, Height - 20, Height - 20));
                } else if (_ImageAlignment == __ImageAlignment.Middle) {
                    G.DrawImage(_Image, new Rectangle(Convert.ToInt32((Width / 2) - ((Height - 20) / 2)), 10, Height - 20, Height - 20));
                } else {
                    G.DrawImage(_Image, new Rectangle((Width - 10) - (Height - 20), 10, Height - 20, Height - 20));
                }
            }
        } else {
            if (_ShowText) {
                G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(10, 0, Width - 20, Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
            }
        }
        G.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

public class VSSeperator : Control {
    #region Declarations
    private Color _FontColour = Color.FromArgb(153, 153, 153);
    private Color _LineColour = Color.FromArgb(0, 122, 204);
    private Font _Font = new Font("Microsoft Sans Serif", 8);
    private bool _ShowText;
    private StringAlignment _TextAlignment = StringAlignment.Center;
    private __TextLocation _TextLocation = __TextLocation.Left;
    private bool _AddEndNotch = false;
    private bool _UnderlineText = false;
    private bool _ShowTextAboveLine = false;
    private bool _OnlyUnderlineText = false;
    #endregion

    #region Properties

    [Category("Control")]
    public __TextLocation TextLocation {
        get {
            return _TextLocation;
        }
        set {
            _TextLocation = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public StringAlignment TextAlignment {
        get {
            return _TextAlignment;
        }
        set {
            _TextAlignment = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowTextAboveLine {
        get {
            return _ShowTextAboveLine;
        }
        set {
            _ShowTextAboveLine = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool OnlyUnderlineText {
        get {
            return _OnlyUnderlineText;
        }
        set {
            _OnlyUnderlineText = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool UnderlineText {
        get {
            return _UnderlineText;
        }
        set {
            _UnderlineText = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool AddEndNotch {
        get {
            return _AddEndNotch;
        }
        set {
            _AddEndNotch = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowText {
        get {
            return _ShowText;
        }
        set {
            _ShowText = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color LineColour {
        get {
            return _LineColour;
        }
        set {
            _LineColour = value;
        }
    }

    [Category("Colours")]
    public Color FontColour {
        get {
            return _FontColour;
        }
        set {
            _FontColour = value;
        }
    }

    protected override void OnSizeChanged(EventArgs e) {
        base.OnSizeChanged(e);
        if (_ShowText && (Height < Font.Size * 2 + 3)) {
            this.Size = new Size(Width, Convert.ToInt32(Font.Size * 2 + 3));
        }
        Invalidate();
    }

    public enum __TextLocation {
        Left,
        Middle,
        Right
    }

    #endregion

    #region Draw Control

    public VSSeperator() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        this.DoubleBuffered = true;
        this.BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e) {
        var G = e.Graphics;
        G.TextRenderingHint = TextRenderingHint.AntiAlias;
        G.SmoothingMode = SmoothingMode.AntiAlias;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        if (_ShowText && !_ShowTextAboveLine) {
            switch (_TextLocation) {
                case __TextLocation.Left:
                    G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(0, 0, Convert.ToInt32(G.MeasureString(Text, Font).Width + 10), Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                    G.DrawLine(new Pen(_LineColour), new Point(Convert.ToInt32(G.MeasureString(Text, Font).Width + 20), Convert.ToInt32(Height / 2)), new Point(Convert.ToInt32(Width), Convert.ToInt32(Height / 2)));
                    if (_AddEndNotch) {
                        G.DrawLine(new Pen(_LineColour), new Point(Width - 1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(Width - 1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                    }
                    if (_UnderlineText) {
                        G.DrawLine(new Pen(_LineColour), 0, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3, Convert.ToInt32(G.MeasureString(Text, Font).Width + 20), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3);
                        G.DrawLine(new Pen(_LineColour), Convert.ToInt32(G.MeasureString(Text, Font).Width + 20), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3, Convert.ToInt32(G.MeasureString(Text, Font).Width + 20), Convert.ToInt32(Height / 2));
                    }
                    break;
                case __TextLocation.Middle:
                    G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(Convert.ToInt32((Width / 2) - (G.MeasureString(Text, Font).Width / 2.0) - 10), 0, Convert.ToInt32(G.MeasureString(Text, Font).Width) + 10, Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                    G.DrawLine(new Pen(_LineColour), new Point(0, Convert.ToInt32(Height / 2)), new Point((Convert.ToInt32((Width / 2) - (G.MeasureString(Text, Font).Width / 2.0) - 20)), Convert.ToInt32(Height / 2)));
                    G.DrawLine(new Pen(_LineColour), new Point((Convert.ToInt32((Width / 2) + (G.MeasureString(Text, Font).Width / 2.0) + 10)), Convert.ToInt32(Height / 2)), new Point(Width, Convert.ToInt32(Height / 2)));
                    if (_AddEndNotch) {
                        G.DrawLine(new Pen(_LineColour), new Point(Width - 1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(Width - 1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                        G.DrawLine(new Pen(_LineColour), new Point(1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                    }
                    if (_UnderlineText) {
                        G.DrawLine(new Pen(_LineColour), (Convert.ToInt32((Width / 2) - (G.MeasureString(Text, Font).Width / 2.0) - 20)), Convert.ToInt32(Height / 2), (Convert.ToInt32((Width / 2) - (G.MeasureString(Text, Font).Width / 2.0) - 20)), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3);
                        G.DrawLine(new Pen(_LineColour), (Convert.ToInt32((Width / 2) + (G.MeasureString(Text, Font).Width / 2.0) + 10)), Convert.ToInt32(Height / 2), (Convert.ToInt32((Width / 2) + (G.MeasureString(Text, Font).Width / 2.0) + 10)), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3);
                        G.DrawLine(new Pen(_LineColour), (Convert.ToInt32((Width / 2) - (G.MeasureString(Text, Font).Width / 2.0) - 20)), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3, (Convert.ToInt32((Width / 2) + (G.MeasureString(Text, Font).Width / 2.0) + 10)), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3);
                    }
                    break;
                case __TextLocation.Right:
                    G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(Convert.ToInt32(Width - G.MeasureString(Text, Font).Width - 10), 0, Convert.ToInt32(G.MeasureString(Text, Font).Width + 10), Height), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Center });
                    G.DrawLine(new Pen(_LineColour), new Point(0, Convert.ToInt32(Height / 2)), new Point(Convert.ToInt32(Width - G.MeasureString(Text, Font).Width - 20), Convert.ToInt32(Height / 2)));
                    if (_AddEndNotch) {
                        G.DrawLine(new Pen(_LineColour), new Point(1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                    }
                    if (_UnderlineText) {
                        G.DrawLine(new Pen(_LineColour), Convert.ToInt32(Width - G.MeasureString(Text, Font).Width - 20), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3, Width, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3);
                        G.DrawLine(new Pen(_LineColour), Convert.ToInt32(Width - G.MeasureString(Text, Font).Width - 20), Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0) + 3, Convert.ToInt32(Width - G.MeasureString(Text, Font).Width - 20), Convert.ToInt32(Height / 2));
                    }
                    break;
            }
        } else if ((_ShowText) && (_ShowTextAboveLine)) {
            if (_OnlyUnderlineText) {
                G.DrawLine(new Pen(_LineColour), new Point(5, Convert.ToInt32(Height / 2) + 6), new Point(Convert.ToInt32(G.MeasureString(Text, Font).Width + 8), Convert.ToInt32(Height / 2) + 6));
                G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(5, 0, Width - 10, Convert.ToInt32(Height / 2 + 3)), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
            } else {
                G.DrawLine(new Pen(_LineColour), new Point(0, Convert.ToInt32(Height / 2) + 6), new Point(Width, Convert.ToInt32(Height / 2) + 6));
                if (_AddEndNotch) {
                    G.DrawLine(new Pen(_LineColour), new Point(Width - 1, Convert.ToInt32(Height / 2) - 5), new Point(Width - 1, Convert.ToInt32((Height / 2) + 5)));
                    G.DrawLine(new Pen(_LineColour), new Point(1, Convert.ToInt32(Height / 2) - 5), new Point(1, Convert.ToInt32((Height / 2) + 5)));
                }
                G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(5, 0, Width - 10, Convert.ToInt32(Height / 2 + 3)), new StringFormat { Alignment = _TextAlignment, LineAlignment = StringAlignment.Far });
            }
        } else {
            if (_OnlyUnderlineText) {
                G.DrawLine(new Pen(_LineColour), new Point(5, Convert.ToInt32(Height / 2) + 6), new Point(Convert.ToInt32(G.MeasureString(Text, Font).Width + 8), Convert.ToInt32(Height / 2) + 6));
                G.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(5, 0, Width - 10, Convert.ToInt32(Height / 2 + 3)), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
            } else {
                G.DrawLine(new Pen(_LineColour), new Point(0, Convert.ToInt32(Height / 2)), new Point(Width, Convert.ToInt32(Height / 2)));
                if (_AddEndNotch) {
                    G.DrawLine(new Pen(_LineColour), new Point(Width - 1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(Width - 1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                    G.DrawLine(new Pen(_LineColour), new Point(1, Convert.ToInt32((Height / 2) - G.MeasureString(Text, Font).Height / 2.0)), new Point(1, Convert.ToInt32((Height / 2) + G.MeasureString(Text, Font).Height / 2.0)));
                }
            }
        }
        G.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

public class VSStatusBar : Control {
    #region Variables
    private Color _TextColour = Color.FromArgb(255, 255, 255);
    private Color _BaseColour = Color.FromArgb(0, 122, 204); //Color.FromArgb(45, 45, 48);
    private Color _WaitingBaseColour = Color.FromArgb(104, 33, 122);
    private Color _RectColour = Color.FromArgb(45, 45, 48);  //Color.FromArgb(0, 122, 204);
    private Color _BorderColour = Color.FromArgb(27, 27, 29);
    private Color _SeperatorColour = Color.FromArgb(45, 45, 48);
    private bool _ShowLine = true;
    private LinesCount _LinesToShow = LinesCount.One;
    private AmountOfStrings _NumberOfStrings = AmountOfStrings.One;
    private bool _ShowBorder = true;
    private StringFormat _FirstLabelStringFormat;
    private string _FirstLabelText = "Label1";
    private Alignments _FirstLabelAlignment = Alignments.Left;
    private StringFormat _SecondLabelStringFormat;
    private string _SecondLabelText = "Label2";
    private Alignments _SecondLabelAlignment = Alignments.Center;
    private StringFormat _ThirdLabelStringFormat;
    private string _ThirdLabelText = "Label3";
    private Alignments _ThirdLabelAlignment = Alignments.Center;
    #endregion

    #region Properties

    [Category("First Label Options")]
    public string FirstLabelText {
        get {
            return _FirstLabelText;
        }
        set {
            _FirstLabelText = value;
            Invalidate();
        }
    }

    [Category("First Label Options")]
    public Alignments FirstLabelAlignment {
        get {
            return _FirstLabelAlignment;
        }
        set {
            switch (value) {
                case Alignments.Left:
                    _FirstLabelStringFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _FirstLabelAlignment = value;
                    break;
                case Alignments.Center:
                    _FirstLabelStringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _FirstLabelAlignment = value;
                    break;
                case Alignments.Right:
                    _FirstLabelStringFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _FirstLabelAlignment = value;
                    break;
            }
        }
    }

    [Category("Second Label Options")]
    public string SecondLabelText {
        get {
            return _SecondLabelText;
        }
        set {
            _SecondLabelText = value;
            Invalidate();
        }
    }

    [Category("Second Label Options")]
    public Alignments SecondLabelAlignment {
        get {
            return _SecondLabelAlignment;
        }
        set {
            switch (value) {
                case Alignments.Left:
                    _SecondLabelStringFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _SecondLabelAlignment = value;
                    break;
                case Alignments.Center:
                    _SecondLabelStringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _SecondLabelAlignment = value;
                    break;
                case Alignments.Right:
                    _SecondLabelStringFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _SecondLabelAlignment = value;
                    break;
            }
        }
    }

    [Category("Third Label Options")]
    public string ThirdLabelText {
        get {
            return _ThirdLabelText;
        }
        set {
            _ThirdLabelText = value;
            Invalidate();
        }
    }

    [Category("Third Label Options")]
    public Alignments ThirdLabelAlignment {
        get {
            return _ThirdLabelAlignment;
        }
        set {
            switch (value) {
                case Alignments.Left:
                    _ThirdLabelStringFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _ThirdLabelAlignment = value;
                    break;
                case Alignments.Center:
                    _ThirdLabelStringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _ThirdLabelAlignment = value;
                    break;
                case Alignments.Right:
                    _ThirdLabelStringFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    _ThirdLabelAlignment = value;
                    break;
            }
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color WaitingBaseColour {
        get {
            return _WaitingBaseColour;
        }
        set {
            _WaitingBaseColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color TextColour {
        get {
            return _TextColour;
        }
        set {
            _TextColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color SeperatorColour {
        get {
            return _SeperatorColour;
        }
        set {
            _SeperatorColour = value;
            Invalidate();
        }
    }

    public enum LinesCount : int {
        None = 0,
        One = 1,
        Two = 2
    }

    public enum AmountOfStrings {
        One,
        Two,
        Three
    }

    public enum Alignments {
        Left,
        Center,
        Right
    }
    private bool _showImage = true;
    public bool showImage {
        get => _showImage;
        set {
            _showImage = value;
            Invalidate();
        }
    }
    private Image _imageToShow;
    public Image imagetoShow {
        get => _imageToShow;
        set {
            _imageToShow = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public AmountOfStrings AmountOfString {
        get {
            return _NumberOfStrings;
        }
        set {
            _NumberOfStrings = value;
        }
    }

    [Category("Control")]
    public LinesCount LinesToShow {
        get {
            return _LinesToShow;
        }
        set {
            _LinesToShow = value;
        }
    }

    public bool ShowBorder {
        get {
            return _ShowBorder;
        }
        set {
            _ShowBorder = value;
        }
    }

    protected override void CreateHandle() {
        base.CreateHandle();
        Dock = DockStyle.Bottom;
    }

    [Category("Colours")]
    public Color RectangleColor {
        get {
            return _RectColour;
        }
        set {
            _RectColour = value;
        }
    }

    public bool ShowLine {
        get {
            return _ShowLine;
        }
        set {
            _ShowLine = value;
        }
    }

    #endregion

    #region Draw Control

    public VSStatusBar() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9);
        Size = new Size(Width, 20);
        Cursor = Cursors.Arrow;
    }

    protected override void OnPaint(PaintEventArgs e) {
        var G = e.Graphics;
        Rectangle Base = new Rectangle(0, 0, Width, Height);
        G.SmoothingMode = SmoothingMode.HighQuality;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        G.FillRectangle(new SolidBrush(_FirstLabelText.EndsWith("...") ? WaitingBaseColour : BaseColour), Base);
        switch (_LinesToShow) {
            case LinesCount.None:
                if (_NumberOfStrings == AmountOfStrings.One) {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(5, 1, Width - 5, Height), _FirstLabelStringFormat);
                } else if (_NumberOfStrings == AmountOfStrings.Two) {
                    var FirstRectangle = new Rectangle(5, 1, Convert.ToInt32((Width / 2 - 6)), Height);
                    var SecondRectangle = new Rectangle(Convert.ToInt32(Width - (Width / 2 + 5)), 1, Convert.ToInt32(Width / 2 - 4), Height);
                    var PenPointA = new Point(Convert.ToInt32(Width / 2), 6);
                    var PenPointB = new Point(Convert.ToInt32(Width - (Width / 3) * 2), Height - 6);
                    if (_SecondLabelAlignment == Alignments.Right) {
                        var Measured = G.MeasureString(_FirstLabelText, Font, FirstRectangle.Width, _FirstLabelStringFormat);
                        if (Measured.Width < FirstRectangle.Width) {
                            FirstRectangle.Width = (int)Measured.Width + 1;
                            PenPointA = new Point(FirstRectangle.X + FirstRectangle.Width + 1, 6);
                            PenPointB = new Point(FirstRectangle.X + FirstRectangle.Width + 1, Height - 6);
                            SecondRectangle = new Rectangle(PenPointA.X + 4, SecondRectangle.Y, Width - (PenPointA.X + 4), SecondRectangle.Height);
                        }
                    }
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), FirstRectangle, _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), SecondRectangle, _SecondLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), PenPointA, PenPointB);
                } else {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(5, 1, Convert.ToInt32((Width - (Width / 3) * 2) - 6), Height), _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) * 2 + 5), 1, Convert.ToInt32(Width - (Width / 3) * 2 - 6), Height), _SecondLabelStringFormat);
                    G.DrawString(_ThirdLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) + 5), 1, Convert.ToInt32(Width / 3 - 6), Height), _ThirdLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3) * 2), 6), new Point(Convert.ToInt32(Width - (Width / 3) * 2), Height - 6));
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3)), 6), new Point(Convert.ToInt32(Width - (Width / 3)), Height - 6));
                }
                break;
            case LinesCount.One:
                if (_NumberOfStrings == AmountOfStrings.One) {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(22, 1, Width, Height), _FirstLabelStringFormat);
                } else if (_NumberOfStrings == AmountOfStrings.Two) {
                    var FirstRectangle = new Rectangle(22, 1, Convert.ToInt32((Width / 2 - 24)), Height);
                    var SecondRectangle = new Rectangle(Convert.ToInt32((Width / 2 + 5)), 1, Convert.ToInt32(Width / 2 - 10), Height);
                    var PenPointA = new Point(Convert.ToInt32(Width / 2), 6);
                    var PenPointB = new Point(Convert.ToInt32(Width - (Width / 3) * 2), Height - 6);
                    if (_SecondLabelAlignment == Alignments.Right) {
                        var Measured = G.MeasureString(_FirstLabelText, Font, FirstRectangle.Width, _FirstLabelStringFormat);
                        if (Measured.Width < FirstRectangle.Width) {
                            FirstRectangle.Width = (int)Measured.Width + 1;
                            PenPointA = new Point(FirstRectangle.X + FirstRectangle.Width + 1, 6);
                            PenPointB = new Point(FirstRectangle.X + FirstRectangle.Width + 1, Height - 6);
                            SecondRectangle = new Rectangle(PenPointA.X + 4, SecondRectangle.Y, Width - (PenPointA.X + 4), SecondRectangle.Height);
                        }
                    }
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), FirstRectangle, _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), SecondRectangle, _SecondLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), PenPointA, PenPointB);
                } else {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(22, 1, Convert.ToInt32((Width - 78) / 3), Height), _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) * 2 + 5), 1, Convert.ToInt32(Width - (Width / 3) * 2 - 12), Height), _SecondLabelStringFormat);
                    G.DrawString(_ThirdLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) + 6), 1, Convert.ToInt32(Width / 3 - 22), Height), _ThirdLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3) * 2), 6), new Point(Convert.ToInt32(Width - (Width / 3) * 2), Height - 6));
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3)), 6), new Point(Convert.ToInt32(Width - (Width / 3)), Height - 6));
                }
                //Icon
                if (_showImage == true && _imageToShow != null)
                    //G.FillRectangle(new SolidBrush(_RectColour), new Rectangle(5, 10, 14, 3));
                    G.DrawImage(new Bitmap(_imageToShow), new Rectangle(2, 10, 15, 15));


                break;
            case LinesCount.Two:
                if (_NumberOfStrings == AmountOfStrings.One) {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(22, 1, Width - 44, Height), _FirstLabelStringFormat);
                } else if (_NumberOfStrings == AmountOfStrings.Two) {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(22, 1, Convert.ToInt32((Width - 46) / 2), Height), _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32((Width / 2 + 5)), 1, Convert.ToInt32(Width / 2 - 28), Height), _SecondLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width / 2), 6), new Point(Convert.ToInt32(Width / 2), Height - 6));
                } else {
                    G.DrawString(_FirstLabelText, Font, new SolidBrush(_TextColour), new Rectangle(22, 1, Convert.ToInt32((Width - 78) / 3), Height), _FirstLabelStringFormat);
                    G.DrawString(_SecondLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) * 2 + 5), 1, Convert.ToInt32(Width - (Width / 3) * 2 - 12), Height), _SecondLabelStringFormat);
                    G.DrawString(_ThirdLabelText, Font, new SolidBrush(_TextColour), new Rectangle(Convert.ToInt32(Width - (Width / 3) + 6), 1, Convert.ToInt32(Width / 3 - 22), Height), _ThirdLabelStringFormat);
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3) * 2), 6), new Point(Convert.ToInt32(Width - (Width / 3) * 2), Height - 6));
                    G.DrawLine(new Pen(_SeperatorColour, 1F), new Point(Convert.ToInt32(Width - (Width / 3)), 6), new Point(Convert.ToInt32(Width - (Width / 3)), Height - 6));
                }
                G.FillRectangle(new SolidBrush(_SeperatorColour), new Rectangle(5, 10, 14, 3));
                G.FillRectangle(new SolidBrush(_SeperatorColour), new Rectangle(Width - 20, 10, 14, 3));
                break;
        }
        if (_ShowBorder) {
            G.DrawRectangle(new Pen(_BorderColour, 2F), new Rectangle(0, 0, Width, Height));
        } else {
        }
        G.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

[DefaultEvent("Scroll")]
public class VSVerticalScrollBar : Control {
    #region Declarations

    private Color _BaseColour = Color.FromArgb(62, 62, 66);
    private Color _ThumbNormalColour = Color.FromArgb(104, 104, 104);
    private Color _ThumbHoverColour = Color.FromArgb(158, 158, 158);
    private Color _ThumbPressedColour = Color.FromArgb(239, 235, 239);
    private Color _ArrowNormalColour = Color.FromArgb(153, 153, 153);
    private Color _ArrowHoveerColour = Color.FromArgb(39, 123, 181);
    private Color _ArrowPressedColour = Color.FromArgb(0, 113, 171);
    private Color _OuterBorderColour;
    private Color _ThumbBorderColour;
    public int _Minimum = 0;
    public int _Maximum = 100;
    private int _Value = 0;
    public int _SmallChange = 1;
    private int _ButtonSize = 16;
    public int _LargeChange = 10;
    private bool _ShowOuterBorder = false;
    private bool _ShowThumbBorder = false;
    private __InnerLineCount _AmountOfInnerLines = __InnerLineCount.None;
    private Point _MousePos = new Point(_MouseXLoc, _MouseYLoc);
    private MouseState _ThumbState = MouseState.None;
    private MouseState _ArrowState = MouseState.None;
    private static int _MouseXLoc;
    private static int _MouseYLoc;
    private int ThumbMovement;
    private Rectangle TSA;
    private Rectangle BSA;
    private Rectangle Shaft;
    private Rectangle Thumb;
    private bool ShowThumb;
    private bool ThumbPressed;
    private int _ThumbSize = 24;

    #endregion

    #region Properties & Events

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbNormalColour {
        get {
            return _ThumbNormalColour;
        }
        set {
            _ThumbNormalColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbHoverColour {
        get {
            return _ThumbHoverColour;
        }
        set {
            _ThumbHoverColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbPressedColour {
        get {
            return _ThumbPressedColour;
        }
        set {
            _ThumbPressedColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowNormalColour {
        get {
            return _ArrowNormalColour;
        }
        set {
            _ArrowNormalColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowHoveerColour {
        get {
            return _ArrowHoveerColour;
        }
        set {
            _ArrowHoveerColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowPressedColour {
        get {
            return _ArrowPressedColour;
        }
        set {
            _ArrowPressedColour = value;
        }
    }

    [Category("Colours")]
    public Color OuterBorderColour {
        get {
            return _OuterBorderColour;
        }
        set {
            _OuterBorderColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbBorderColour {
        get {
            return _ThumbBorderColour;
        }
        set {
            _ThumbBorderColour = value;
        }
    }

    [Category("Control")]
    public int Minimum {
        get {
            return _Minimum;
        }
        set {
            _Minimum = value;
            if (value > _Value) {
                _Value = value;
            }
            if (value > _Maximum) {
                _Maximum = value;
            }
            _Minimum = value;
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public int Maximum {
        get {
            return _Maximum;
        }
        set {
            if (value < _Value) {
                _Value = value;
            }
            if (value < _Minimum) {
                _Minimum = value;
            }
            _Maximum = value;
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public int Value {
        get {
            return _Value;
        }
        set {
            if (value == _Value) {
                return;
            }
            else if (value < _Minimum) {
                _Value = _Minimum;
            }
            else if (value > _Maximum) {
                _Value = _Maximum;
            }
            else {
                _Value = value;
            }
            InvalidatePosition();
            if (Scroll != null)
                Scroll(this);
        }
    }

    [Category("Control")]
    public int SmallChange {
        get {
            return _SmallChange;
        }
        set {
            if (value < 1)
                _SmallChange = 1;
            else
                _SmallChange = value;
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public int LargeChange {
        get {
            return _LargeChange;
        }
        set {
            if (value < 1)
                _LargeChange = 1;            
            else
                _LargeChange = value;
            InvalidateLayout();            
        }
    }

    [Category("Control")]
    public int ButtonSize {
        get {
            return _ButtonSize;
        }
        set {
            if (value < 16) {
                _ButtonSize = 16;
            }
            else {
                _ButtonSize = value;
            }
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public bool ShowOuterBorder {
        get {
            return _ShowOuterBorder;
        }
        set {
            _ShowOuterBorder = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowThumbBorder {
        get {
            return _ShowThumbBorder;
        }
        set {
            _ShowThumbBorder = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public __InnerLineCount AmountOfInnerLines {
        get {
            return _AmountOfInnerLines;
        }
        set {
            _AmountOfInnerLines = value;
        }
    }

    protected override void OnSizeChanged(EventArgs e) {
        InvalidateLayout();
    }

    private void InvalidateLayout() {
        //'End Height here goes with end in invalidateposition() for starting height of thumb
        TSA = new Rectangle(0, 0, Width, 16);
        BSA = new Rectangle(0, Height - ButtonSize, Width, ButtonSize);
        //'End height here should be double the start for symetry
        Shaft = new Rectangle(0, TSA.Bottom + 1, Width, Convert.ToInt32(Height - Height / 8 - 8));
        ShowThumb = Convert.ToBoolean(((_Maximum - _Minimum)));
        if (ShowThumb) {
            Thumb = new Rectangle(4, 0, Width - 8, Convert.ToInt32(Height / 8));
        }
        if (Scroll != null)
            Scroll(this);
        InvalidatePosition();
    }

    public enum __InnerLineCount {
        None,
        One,
        Two,
        Three
    }

    public delegate void ScrollEventHandler(object sender);
    public event ScrollEventHandler Scroll;

    public void InvalidatePosition() {
        Thumb.Y = Convert.ToInt32(((_Value - _Minimum) / (double)(_Maximum - _Minimum)) * (Shaft.Height - _ThumbSize) + 16);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        if (e.Button == System.Windows.Forms.MouseButtons.Left && ShowThumb) {
            if (TSA.Contains(e.Location)) {
                _ArrowState = MouseState.Down;
                ThumbMovement = _Value - _SmallChange;
            } else if (BSA.Contains(e.Location)) {
                ThumbMovement = _Value + _SmallChange;
                _ArrowState = MouseState.Down;
            } else {
                if (Thumb.Contains(e.Location)) {
                    _ThumbState = MouseState.Down;
                    Invalidate();
                    return;
                } else {
                    if (e.Y < Thumb.Y) {
                        ThumbMovement = _Value - _LargeChange;
                    } else {
                        ThumbMovement = _Value + _LargeChange;
                    }
                }
            }
            Value = Math.Min(Math.Max(ThumbMovement, _Minimum), _Maximum);
            Invalidate();
            InvalidatePosition();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        _MouseXLoc = e.Location.X;
        _MouseYLoc = e.Location.Y;
        if (TSA.Contains(e.Location)) {
            _ArrowState = MouseState.Over;
        } else if (BSA.Contains(e.Location)) {
            _ArrowState = MouseState.Over;
        } else if (!(_ArrowState == MouseState.Down)) {
            _ArrowState = MouseState.None;
        }
        if (Thumb.Contains(e.Location) && !(_ThumbState == MouseState.Down)) {
            _ThumbState = MouseState.Over;
        } else if (!(_ThumbState == MouseState.Down)) {
            _ThumbState = MouseState.None;
        }
        Invalidate();
        if (_ThumbState == MouseState.Down || _ArrowState == MouseState.Down && ShowThumb) {
            int ThumbPosition = e.Y + 2 - TSA.Height - (_ThumbSize / 2);
            int ThumbBounds = Shaft.Height - _ThumbSize;
            ThumbMovement = Convert.ToInt32((ThumbPosition / (double)ThumbBounds) * (_Maximum - _Minimum)) - _Minimum;
            Value = Math.Min(Math.Max(ThumbMovement, _Minimum), _Maximum);
            InvalidatePosition();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        if (Thumb.Contains(e.Location)) {
            _ThumbState = MouseState.Over;
        } else if (!(Thumb.Contains(e.Location))) {
            _ThumbState = MouseState.None;
        }
        if (e.Location.Y < 16 || e.Location.Y > Width - 16) {
            _ThumbState = MouseState.Over;
        } else if (!(e.Location.Y < 16) || e.Location.Y > Width - 16) {
            _ThumbState = MouseState.None;
        }
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        _ThumbState = MouseState.None;
        _ArrowState = MouseState.None;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) {
        base.OnMouseEnter(e);
        Invalidate();
    }

    #endregion

    #region Draw Control

    public VSVerticalScrollBar() {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        Size = new Size(19, 50);
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(_BaseColour);
        Point[] TrianglePointTop = {
            new Point(Convert.ToInt32(Width / 2), 5),
            new Point(Convert.ToInt32(Width / 4), 11),
            new Point(Convert.ToInt32(Width / 2 + Width / 4), 11)
        };
        Point[] TrianglePointBottom = {
            new Point(Convert.ToInt32(Width / 2), Height - 5),
            new Point(Convert.ToInt32(Width / 4), Height - 11),
            new Point(Convert.ToInt32(Width / 2 + Width / 4), Height - 11)
        };
        switch (_ThumbState) {
            case MouseState.None:
                using (SolidBrush SBrush = new SolidBrush(_ThumbNormalColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
            case MouseState.Over:
                using (SolidBrush SBrush = new SolidBrush(_ThumbHoverColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
            case MouseState.Down:
                using (SolidBrush SBrush = new SolidBrush(_ThumbPressedColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
        }
        switch (_ArrowState) {
            case MouseState.Down:
                if (!(Thumb.Contains(_MousePos))) {
                    using (SolidBrush SBrush = new SolidBrush(_ThumbNormalColour)) {
                        g.FillRectangle(SBrush, Thumb);
                    }
                }
                if (_MouseYLoc < 16) {
                    g.FillPolygon(new SolidBrush(_ArrowPressedColour), TrianglePointTop);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointBottom);

                } else if (_MouseXLoc > Width - 16) {
                    g.FillPolygon(new SolidBrush(_ArrowPressedColour), TrianglePointBottom);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointTop);
                } else {
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointTop);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointBottom);
                }
                break;
            case MouseState.Over:

                if (_MouseYLoc < 16) {
                    g.FillPolygon(new SolidBrush(_ArrowHoveerColour), TrianglePointTop);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointBottom);
                } else if (_MouseXLoc > Width - 16) {
                    g.FillPolygon(new SolidBrush(_ArrowHoveerColour), TrianglePointBottom);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointTop);
                } else {
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointTop);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointBottom);
                }
                break;
            case MouseState.None:

                g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointTop);
                g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointBottom);
                break;
        }
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

public class VSHorizontalScrollBar : Control {
    #region Declarations

    private Color _BaseColour = Color.FromArgb(62, 62, 66);
    private Color _ThumbNormalColour = Color.FromArgb(104, 104, 104);
    private Color _ThumbHoverColour = Color.FromArgb(158, 158, 158);
    private Color _ThumbPressedColour = Color.FromArgb(239, 235, 239);
    private Color _ArrowNormalColour = Color.FromArgb(153, 153, 153);
    private Color _ArrowHoveerColour = Color.FromArgb(39, 123, 181);
    private Color _ArrowPressedColour = Color.FromArgb(0, 113, 171);
    private Color _OuterBorderColour;
    private Color _ThumbBorderColour;
    private int _Minimum = 0;
    private int _Maximum = 100;
    private int _Value = 0;
    private int _SmallChange = 1;
    private int _ButtonSize = 16;
    private int _LargeChange = 10;
    private bool _ShowOuterBorder = false;
    private bool _ShowThumbBorder = false;
    private __InnerLineCount _AmountOfInnerLines = __InnerLineCount.None;
    private Point _MousePos = new Point(_MouseXLoc, _MouseYLoc);
    private MouseState _ThumbState = MouseState.None;
    private MouseState _ArrowState = MouseState.None;
    private static int _MouseXLoc;
    private static int _MouseYLoc;
    private int ThumbMovement;
    private Rectangle LSA;
    private Rectangle RSA;
    private Rectangle Shaft;
    private Rectangle Thumb;
    private bool ShowThumb;
    private bool ThumbPressed;
    private int _ThumbSize = 24;


    #endregion

    #region Properties & Events

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbNormalColour {
        get {
            return _ThumbNormalColour;
        }
        set {
            _ThumbNormalColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbHoverColour {
        get {
            return _ThumbHoverColour;
        }
        set {
            _ThumbHoverColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbPressedColour {
        get {
            return _ThumbPressedColour;
        }
        set {
            _ThumbPressedColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowNormalColour {
        get {
            return _ArrowNormalColour;
        }
        set {
            _ArrowNormalColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowHoveerColour {
        get {
            return _ArrowHoveerColour;
        }
        set {
            _ArrowHoveerColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowPressedColour {
        get {
            return _ArrowPressedColour;
        }
        set {
            _ArrowPressedColour = value;
        }
    }

    [Category("Colours")]
    public Color OuterBorderColour {
        get {
            return _OuterBorderColour;
        }
        set {
            _OuterBorderColour = value;
        }
    }

    [Category("Colours")]
    public Color ThumbBorderColour {
        get {
            return _ThumbBorderColour;
        }
        set {
            _ThumbBorderColour = value;
        }
    }

    [Category("Control")]
    public int Minimum {
        get {
            return _Minimum;
        }
        set {
            _Minimum = value;
            if (value > _Value) {
                _Value = value;
            }
            if (value > _Maximum) {
                _Maximum = value;
            }
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public int Maximum {
        get {
            return _Maximum;
        }
        set {
            if (value < _Value) {
                _Value = value;
            }
            if (value < _Minimum) {
                _Minimum = value;
            }
            InvalidateLayout();
        }
    }

    [Category("Control")]
    public int Value {
        get {
            return _Value;
        }
        set {
            if (value == _Value) {
                return;
            }
            else if (value < _Minimum) {
                _Value = _Minimum;
            }
            else if (value > _Maximum) {
                _Value = _Maximum;
            }
            else {
                _Value = value;
            }
            InvalidatePosition();
            if (Scroll != null)
                Scroll(this);
        }
    }

    [Category("Control")]
    public int SmallChange {
        get {
            return _SmallChange;
        }
        set {
            if (value < 1)
                _SmallChange = 1;
            else
                _SmallChange = value;
        }
    }

    [Category("Control")]
    public int LargeChange {
        get {
            return _LargeChange;
        }
        set {
            if (value < 1) 
                _LargeChange = 1;            
            else 
                _LargeChange = value;            
        }
    }

    [Category("Control")]
    public int ButtonSize {
        get {
            return _ButtonSize;
        }
        set {
            if (value < 16) {
                _ButtonSize = 16;
            }
            else {
                _ButtonSize = value;
            }
        }
    }

    [Category("Control")]
    public bool ShowOuterBorder {
        get {
            return _ShowOuterBorder;
        }
        set {
            _ShowOuterBorder = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowThumbBorder {
        get {
            return _ShowThumbBorder;
        }
        set {
            _ShowThumbBorder = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public __InnerLineCount AmountOfInnerLines {
        get {
            return _AmountOfInnerLines;
        }
        set {
            _AmountOfInnerLines = value;
        }
    }

    protected override void OnSizeChanged(EventArgs e) {
        InvalidateLayout();
    }

    private void InvalidateLayout() {

        //'End width here goes with end in invalidateposition() for starting height of thumb
        LSA = new Rectangle(0, 0, 16, Height);
        RSA = new Rectangle(Width - ButtonSize, 0, ButtonSize, Height);
        //'End width here should be double the start for symetry
        Shaft = new Rectangle(LSA.Right + 1, 0, Convert.ToInt32(Width - Width / 8 - 8), Height);
        ShowThumb = Convert.ToBoolean(((_Maximum - _Minimum)));
        if (ShowThumb) {
            Thumb = new Rectangle(0, 4, Convert.ToInt32(Width / 8), Height - 8);
        }
        if (Scroll != null)
            Scroll(this);
        InvalidatePosition();
    }

    public enum __InnerLineCount {
        None,
        One,
        Two,
        Three
    }

    public delegate void ScrollEventHandler(object sender);
    public event ScrollEventHandler Scroll;

    private void InvalidatePosition() {
        Thumb.X = Convert.ToInt32(((_Value - _Minimum) / (double)(_Maximum - _Minimum)) * (Shaft.Width - _ThumbSize) + 16);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        if (e.Button == System.Windows.Forms.MouseButtons.Left && ShowThumb) {
            if (LSA.Contains(e.Location)) {
                _ArrowState = MouseState.Down;
                ThumbMovement = _Value - _SmallChange;
            } else if (RSA.Contains(e.Location)) {
                ThumbMovement = _Value + _SmallChange;
                _ArrowState = MouseState.Down;
            } else {
                if (Thumb.Contains(e.Location)) {
                    _ThumbState = MouseState.Down;
                    Invalidate();
                    return;
                } else {
                    if (e.X < Thumb.X) {
                        ThumbMovement = _Value - _LargeChange;
                    } else {
                        ThumbMovement = _Value + _LargeChange;
                    }
                }
            }
            Value = Math.Min(Math.Max(ThumbMovement, _Minimum), _Maximum);
            Invalidate();
            InvalidatePosition();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        _MouseXLoc = e.Location.X;
        _MouseYLoc = e.Location.Y;
        if (LSA.Contains(e.Location)) {
            _ArrowState = MouseState.Over;
        } else if (RSA.Contains(e.Location)) {
            _ArrowState = MouseState.Over;
        } else if (!(_ArrowState == MouseState.Down)) {
            _ArrowState = MouseState.None;
        }
        if (Thumb.Contains(e.Location) && !(_ThumbState == MouseState.Down)) {
            _ThumbState = MouseState.Over;
        } else if (!(_ThumbState == MouseState.Down)) {
            _ThumbState = MouseState.None;
        }
        Invalidate();
        if (_ThumbState == MouseState.Down || _ArrowState == MouseState.Down && ShowThumb) {
            int ThumbPosition = e.X + 2 - LSA.Width - (_ThumbSize / 2);
            int ThumbBounds = Shaft.Width - _ThumbSize;
            ThumbMovement = Convert.ToInt32((ThumbPosition / (double)ThumbBounds) * (_Maximum - _Minimum)) - _Minimum;
            Value = Math.Min(Math.Max(ThumbMovement, _Minimum), _Maximum);
            InvalidatePosition();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        if (Thumb.Contains(e.Location)) {
            _ThumbState = MouseState.Over;
        } else if (!(Thumb.Contains(e.Location))) {
            _ThumbState = MouseState.None;
        }
        if (e.Location.X < 16 || e.Location.X > Width - 16) {
            _ThumbState = MouseState.Over;
        } else if (!(e.Location.X < 16) || e.Location.X > Width - 16) {
            _ThumbState = MouseState.None;
        }
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        _ThumbState = MouseState.None;
        _ArrowState = MouseState.None;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) {
        base.OnMouseEnter(e);
        Invalidate();
    }

    #endregion

    #region Draw Control

    public VSHorizontalScrollBar() {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        Size = new Size(50, 19);
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(_BaseColour);
        Point[] TrianglePointLeft = {
            new Point(5, Convert.ToInt32(Height / 2)),
            new Point(11, Convert.ToInt32(Height / 4)),
            new Point(11, Convert.ToInt32(Height / 2 + Height / 4))
        };
        Point[] TrianglePointRight = {
            new Point(Width - 5, Convert.ToInt32(Height / 2)),
            new Point(Width - 11, Convert.ToInt32(Height / 4)),
            new Point(Width - 11, Convert.ToInt32(Height / 2 + Height / 4))
        };
        switch (_ThumbState) {
            case MouseState.None:
                using (SolidBrush SBrush = new SolidBrush(_ThumbNormalColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
            case MouseState.Over:
                using (SolidBrush SBrush = new SolidBrush(_ThumbHoverColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
            case MouseState.Down:
                using (SolidBrush SBrush = new SolidBrush(_ThumbPressedColour)) {
                    g.FillRectangle(SBrush, Thumb);
                }
                break;
        }
        switch (_ArrowState) {
            case MouseState.Down:
                if (!(Thumb.Contains(_MousePos))) {
                    using (SolidBrush SBrush = new SolidBrush(_ThumbNormalColour)) {
                        g.FillRectangle(SBrush, Thumb);
                    }
                }
                if (_MouseXLoc < 16) {
                    g.FillPolygon(new SolidBrush(_ArrowPressedColour), TrianglePointLeft);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointRight);

                } else if (_MouseXLoc > Width - 16) {
                    g.FillPolygon(new SolidBrush(_ArrowPressedColour), TrianglePointRight);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointLeft);
                } else {
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointLeft);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointRight);
                }
                break;
            case MouseState.Over:

                if (_MouseXLoc < 16) {
                    g.FillPolygon(new SolidBrush(_ArrowHoveerColour), TrianglePointLeft);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointRight);
                } else if (_MouseXLoc > Width - 16) {
                    g.FillPolygon(new SolidBrush(_ArrowHoveerColour), TrianglePointRight);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointLeft);
                } else {
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointLeft);
                    g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointRight);
                }
                break;
            case MouseState.None:

                g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointLeft);
                g.FillPolygon(new SolidBrush(_ArrowNormalColour), TrianglePointRight);
                break;
        }
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion
}

public class VSListBoxWBuiltInScrollBar : Control {
    #region Declarations

    private List<VSListBoxItem> _Items = new List<VSListBoxItem>();
    private readonly List<VSListBoxItem> _SelectedItems = new List<VSListBoxItem>();
    private bool _MultiSelect = true;
    private int ItemHeight = 24;
    private VSVerticalScrollBar VerticalScrollbar;
    private Color _BaseColour = Color.FromArgb(37, 37, 38);
    private Color _NonSelectedItemColour = Color.FromArgb(62, 62, 64);
    private Color _SelectedItemColour = Color.FromArgb(47, 47, 47);
    private Color _BorderColour = Color.FromArgb(35, 35, 35);
    private Color _FontColour = Color.FromArgb(199, 199, 199);
    private int _SelectedWidth = 1;
    private int _SelectedHeight = 1;
    private bool _DontShowInnerScrollbarBorder = false;
    private bool _ShowWholeInnerBorder = true;

    #endregion

    #region Properties

    [Category("Colours")]
    public Color FontColour {
        get {
            return _FontColour;
        }
        set {
            _FontColour = value;
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color SelectedItemColour {
        get {
            return _SelectedItemColour;
        }
        set {
            _SelectedItemColour = value;
        }
    }

    [Category("Colours")]
    public Color NonSelectedItemColour {
        get {
            return _NonSelectedItemColour;
        }
        set {
            _NonSelectedItemColour = value;
        }
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    [Category("Control")]
    public int SelectedHeight {
        get {
            return _SelectedHeight;
        }
    }

    [Category("Control")]
    public int SelectedWidth {
        get {
            return _SelectedWidth;
        }
    }

    [Category("Control")]
    public bool DontShowInnerScrollbarBorder {
        get {
            return _DontShowInnerScrollbarBorder;
        }
        set {
            _DontShowInnerScrollbarBorder = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowWholeInnerBorder {
        get {
            return _ShowWholeInnerBorder;
        }
        set {
            _ShowWholeInnerBorder = value;
            Invalidate();
        }
    }

    [Category("Control"), System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
    public VSListBoxItem[] Items {
        get {
            return _Items.ToArray();
        }
        set {
            _Items = new List<VSListBoxItem>(value);
            Invalidate();
            InvalidateScroll();
        }
    }

    [Category("Control")]
    public VSListBoxItem[] SelectedItems {
        get {
            return _SelectedItems.ToArray();
        }
    }

    [Category("Control")]
    public bool MultiSelect {
        get {
            return _MultiSelect;
        }
        set {
            _MultiSelect = value;

            if (_SelectedItems.Count > 1) {
                _SelectedItems.RemoveRange(1, _SelectedItems.Count - 1);
            }

            Invalidate();
        }
    }

    private void HandleScroll(object sender) {
        Invalidate();
    }

    private void InvalidateScroll() {/*
        if (Convert.ToInt32(Math.Round(((_Items.Count) * ItemHeight) / (double)_SelectedHeight)) < Convert.ToDouble((((_Items.Count) * ItemHeight) / (double)_SelectedHeight))) {
            VerticalScrollbar._Maximum = Convert.ToInt32(Math.Ceiling(((_Items.Count) * ItemHeight) / (double)_SelectedHeight));
        } else if (Convert.ToInt32(Math.Round(((_Items.Count) * ItemHeight) / (double)_SelectedHeight)) == 0) {
            VerticalScrollbar._Maximum = 1;
        } else {
            VerticalScrollbar._Maximum = Convert.ToInt32(Math.Round(((_Items.Count) * ItemHeight) / (double)_SelectedHeight));
        }*/

        int TotalHeight = (_Items.Count) * ItemHeight;

        if (TotalHeight > Height) {
            VerticalScrollbar.Maximum = 100;
            VerticalScrollbar.SmallChange = ItemHeight;
            int StepSize = ((Height / ItemHeight) / 4) * ItemHeight;
            VerticalScrollbar.LargeChange = (int)(StepSize * (100/(double)Height));
            VerticalScrollbar.ButtonSize = TotalHeight / VerticalScrollbar.LargeChange;
        } else
            VerticalScrollbar.Maximum = 1;

        VerticalScrollbar.Visible = VerticalScrollbar._Maximum > 1;
        Invalidate();
    }

    private void InvalidateLayout() {
        VerticalScrollbar.Location = new Point(Width - VerticalScrollbar.Width - 2, 2);
        VerticalScrollbar.Size = new Size(18, Height - 4);
        Invalidate();
    }

    public class VSListBoxItem {
        public string Text { get; set; }
        public override string ToString() {
            return Text;
        }
    }

    public override Font Font {
        get {
            return base.Font;
        }
        set {
            ItemHeight = Convert.ToInt32(Graphics.FromHwnd(Handle).MeasureString("@", Font).Height);
            if (VerticalScrollbar != null) {
                VerticalScrollbar._SmallChange = 1;
                VerticalScrollbar._LargeChange = 1;
            }
            base.Font = value;
            InvalidateLayout();
        }
    }

    public void AddItem(string Items) {
        VSListBoxItem Item = new VSListBoxItem();
        Item.Text = Items;
        _Items.Add(Item);
        Invalidate();
        InvalidateScroll();
    }

    public void AddItems(string[] Items) {
        foreach (var I in Items) {
            VSListBoxItem Item = new VSListBoxItem();
            Item.Text = I;
            _Items.Add(Item);
        }
        Invalidate();
        InvalidateScroll();
    }

    public void RemoveItemAt(int index) {
        _Items.RemoveAt(index);
        Invalidate();
        InvalidateScroll();
    }

    public void RemoveItem(VSListBoxItem item) {
        _Items.Remove(item);
        Invalidate();
        InvalidateScroll();
    }

    public void RemoveItems(VSListBoxItem[] items) {
        foreach (VSListBoxItem I in items) {
            _Items.Remove(I);
        }
        Invalidate();
        InvalidateScroll();
    }

    protected override void OnSizeChanged(EventArgs e) {
        _SelectedWidth = Width;
        _SelectedHeight = Height;
        InvalidateScroll();
        InvalidateLayout();
        base.OnSizeChanged(e);
    }

    private void Vertical_MouseDown(object sender, MouseEventArgs e) {
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        Focus();
        if (e.Button == MouseButtons.Left) {
            int Offset = GetScrollOffset();
            int Index = ((e.Y + Offset) / ItemHeight);
            if (Index > _Items.Count - 1) {
                Index = -1;
            }
            if (!(Index == -1)) {
                if (ModifierKeys == Keys.Control && _MultiSelect) {
                    if (_SelectedItems.Contains(_Items[Index])) {
                        _SelectedItems.Remove(_Items[Index]);
                    } else {
                        _SelectedItems.Add(_Items[Index]);
                    }
                } else {
                    _SelectedItems.Clear();
                    _SelectedItems.Add(_Items[Index]);
                }
            }
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    private int GetScrollOffset() {
        //int Offset = Convert.ToInt32(VerticalScrollbar.Value * (VerticalScrollbar.Maximum + (Height - (ItemHeight))));
        int MaxHeight = (ItemHeight * Items.Length) - Height;
        if (MaxHeight < 0)
            return 0;
        int Offset = (int)(((MaxHeight) * VerticalScrollbar.Value) / (double)100);
        return Offset;
    }
    public int GetElementIndex(Point Point) {
        int Offset = GetScrollOffset();
        int Index = ((Point.Y + Offset) / ItemHeight);
        if (Index > _Items.Count - 1) {
            Index = -1;
        }
        return Index;
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        int Move = (-(e.Delta * SystemInformation.MouseWheelScrollLines / 120) * (2 / 2)) * ItemHeight;
        int Value = Math.Max(Math.Min(VerticalScrollbar.Value + Move, VerticalScrollbar.Maximum), VerticalScrollbar.Minimum);
        VerticalScrollbar.Value = Value;
        base.OnMouseWheel(e);
    }

    #endregion

    #region Draw Control

    public VSListBoxWBuiltInScrollBar() {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        VerticalScrollbar = new VSVerticalScrollBar();
        VerticalScrollbar._SmallChange = 1;
        VerticalScrollbar._LargeChange = 1;
        VerticalScrollbar.Scroll += HandleScroll;
        VerticalScrollbar.MouseDown += Vertical_MouseDown;
        Controls.Add(VerticalScrollbar);
        InvalidateLayout();
    }

    protected override void OnPaint(PaintEventArgs e) {

        var G = e.Graphics;
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        G.SmoothingMode = SmoothingMode.HighQuality;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        G.Clear(_BaseColour);
        VSListBoxItem AllItems = null;
        int Offset = GetScrollOffset();
        int StartIndex = 0;
        if (Offset == 0) {
            StartIndex = 0;
        } else {
            StartIndex = Convert.ToInt32(Offset / ItemHeight);
        }
        int EndIndex = Math.Min(StartIndex + (Height / ItemHeight), _Items.Count - 1);
        if (!_DontShowInnerScrollbarBorder && !_ShowWholeInnerBorder && VerticalScrollbar.Visible) {
            G.DrawLine(new Pen(_BorderColour, 2F), VerticalScrollbar.Location.X - 1, 0, VerticalScrollbar.Location.X - 1, Height);
        }
        for (int I = StartIndex; I < _Items.Count; I++) {
            AllItems = Items[I];
            int Y = ((I * ItemHeight) + 1 - Offset) + Convert.ToInt32((ItemHeight / 2.0) - 8);
            if (_SelectedItems.Contains(AllItems)) {
                G.FillRectangle(new SolidBrush(_SelectedItemColour), new Rectangle(0, (I * ItemHeight) + 1 - Offset, Width - (VerticalScrollbar.Visible ? 19 : 0), ItemHeight - 1));
            } else {
                G.FillRectangle(new SolidBrush(_NonSelectedItemColour), new Rectangle(0, (I * ItemHeight) + 1 - Offset, Width - (VerticalScrollbar.Visible ? 19 : 0), ItemHeight - 1));
            }
            G.DrawLine(new Pen(_BorderColour), 0, ((I * ItemHeight) + 1 - Offset) + ItemHeight - 1, Width - (VerticalScrollbar.Visible ? 18 : 0), ((I * ItemHeight) + 1 - Offset) + ItemHeight - 1);
            if (G.MeasureString(AllItems.Text, new Font("Segoe UI", 8)).Width > (_SelectedWidth) - (VerticalScrollbar.Visible ? 30 : 11)) {
                G.DrawString(AllItems.Text, new Font("Segoe UI", 8), new SolidBrush(_FontColour), new Rectangle(7, Y, Width - (VerticalScrollbar.Visible ? 35 : 16), 15));
                G.DrawString("...", new Font("Segoe UI", 8), new SolidBrush(_FontColour), new Rectangle(Width - (VerticalScrollbar.Visible ? 32 : 13), Y, 15, 15));
            } else {
                G.DrawString(AllItems.Text, new Font("Segoe UI", 8), new SolidBrush(_FontColour), new Rectangle(7, Y, Width - (VerticalScrollbar.Visible ? 34 : 15), Y + 10));
            }
            G.ResetClip();
        }
        G.DrawRectangle(new Pen(Color.FromArgb(35, 35, 35), 2F), 1, 1, Width - 2, Height - 2);
        G.InterpolationMode = (InterpolationMode)7;
        if (_ShowWholeInnerBorder && VerticalScrollbar.Visible) {
            G.DrawLine(new Pen(_BorderColour, 2F), VerticalScrollbar.Location.X - 1, 0, VerticalScrollbar.Location.X - 1, Height);
        }
    }

    #endregion

}

public class VSRadialProgressBar : Control {
    #region Declarations
    private Color _BorderColour = Color.FromArgb(28, 28, 28);
    private Color _BaseColour = Color.FromArgb(45, 45, 48);
    private Color _ProgressColour = Color.FromArgb(62, 62, 66);
    private Color _TextColour = Color.FromArgb(153, 153, 153);
    private int _Value = 0;
    private int _Maximum = 100;
    private int _StartingAngle = 110;
    private int _RotationAngle = 255;
    private bool _ShowText = false;
    private Font _Font = new Font("Segoe UI", 20);
    #endregion

    #region Properties

    [Category("Control")]
    public int Maximum {
        get {
            return _Maximum;
        }
        set {
            if (value < _Value) {
                _Value = value;
            }
            _Maximum = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public int Value {
        get {
            switch (_Value) {
                case 0:
                    return 0;
                    Invalidate();
                    break;
                default:
                    return _Value;
                    Invalidate();
                    break;
            }
        }

        set {
            if (value > _Maximum) {
                value = _Maximum;
                Invalidate();
            }
            _Value = value;
            Invalidate();
        }
    }

    public void Increment(int Amount) {
        Value += Amount;
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
            Invalidate();
        }
    }

    [Category("Colours")]
    public Color TextColour {
        get {
            return _TextColour;
        }
        set {
            _TextColour = value;
            Invalidate();

        }
    }

    [Category("Colours")]
    public Color ProgressColour {
        get {
            return _ProgressColour;
        }
        set {
            _ProgressColour = value;
            Invalidate();

        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
            Invalidate();

        }
    }

    [Category("Control")]
    public int StartingAngle {
        get {
            return _StartingAngle;
        }
        set {
            _StartingAngle = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public int RotationAngle {
        get {
            return _RotationAngle;
        }
        set {
            _RotationAngle = value;
            Invalidate();
        }
    }

    [Category("Control")]
    public bool ShowText {
        get {
            return _ShowText;
        }
        set {
            _ShowText = value;
            Invalidate();
        }
    }

    //Protected Overrides Sub OnSizeChanged(e As EventArgs)
    //    Dim g As Graphics
    //    If Height < g.MeasureString(CStr(_Value), Font).Height * 2 Then
    //        Me.Size = New Size(CInt(g.MeasureString(CStr(_Value), Font).Height * 2), CInt(g.MeasureString(CStr(_Value), Font).Height * 2))
    //    End If
    //    MyBase.OnSizeChanged(e)
    //End Sub

    #endregion

    #region Draw Control
    public VSRadialProgressBar() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        Size = new Size(78, 78);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e) {
        Bitmap B = new Bitmap(Width, Height);
        var G = Graphics.FromImage(B);
        G.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        G.SmoothingMode = SmoothingMode.HighQuality;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
        G.Clear(BackColor);

        if (_Value == 0) {
            G.DrawArc(new Pen(new SolidBrush(_BorderColour), 1 + 6), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle - 3, _RotationAngle + 5);
            G.DrawArc(new Pen(new SolidBrush(_BaseColour), 1 + 3), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle, _RotationAngle);

            if (_ShowText)
                G.DrawString(Convert.ToString(_Value), _Font, new SolidBrush(_TextColour), new Point(Convert.ToInt32(Width / 2), Convert.ToInt32(Height / 2 - 1)), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
        else if (_Value == _Maximum) {
            G.DrawArc(new Pen(new SolidBrush(_BorderColour), 1 + 6), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle - 3, _RotationAngle + 5);
            G.DrawArc(new Pen(new SolidBrush(_BaseColour), 1 + 3), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle, _RotationAngle);
            G.DrawArc(new Pen(new SolidBrush(_ProgressColour), 1 + 3), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle, _RotationAngle);

            if (_ShowText)
                G.DrawString(Convert.ToString(_Value), _Font, new SolidBrush(_TextColour), new Point(Convert.ToInt32(Width / 2), Convert.ToInt32(Height / 2 - 1)), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
        else {
            G.DrawArc(new Pen(new SolidBrush(_BorderColour), 1 + 6), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle - 3, _RotationAngle + 5);
            G.DrawArc(new Pen(new SolidBrush(_BaseColour), 1 + 3), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle, _RotationAngle);
            G.DrawArc(new Pen(new SolidBrush(_ProgressColour), 1 + 3), Convert.ToInt32(3 / 2.0) + 1, Convert.ToInt32(3 / 2.0) + 1, Width - 3 - 4, Height - 3 - 3, _StartingAngle, Convert.ToInt32((_RotationAngle / (double)_Maximum) * _Value));

            if (_ShowText)
                G.DrawString(Convert.ToString(_Value), _Font, new SolidBrush(_TextColour), new Point(Convert.ToInt32(Width / 2), Convert.ToInt32(Height / 2 - 1)), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }

        base.OnPaint(e);
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImageUnscaled(B, 0, 0);
        B.Dispose();
    }
    #endregion

}

public class VSTabControl : TabControl {
    #region Declarations

    private Color _TextColour = Color.FromArgb(255, 255, 255);
    private Color _BackTabColour = Color.FromArgb(28, 28, 28);
    private Color _BaseColour = Color.FromArgb(45, 45, 48);
    private Color _ActiveColour = Color.FromArgb(0, 122, 204);
    private Color _BorderColour = Color.FromArgb(30, 30, 30);
    private Color _HorizLineColour = Color.FromArgb(0, 122, 204);
    private StringFormat CenterSF = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

    #endregion

    #region Properties

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    [Category("Colours")]
    public Color HorizontalLineColour {
        get {
            return _HorizLineColour;
        }
        set {
            _HorizLineColour = value;
        }
    }

    [Category("Colours")]
    public Color TextColour {
        get {
            return _TextColour;
        }
        set {
            _TextColour = value;
        }
    }

    [Category("Colours")]
    public Color BackTabColour {
        get {
            return _BackTabColour;
        }
        set {
            _BackTabColour = value;
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color ActiveColour {
        get {
            return _ActiveColour;
        }
        set {
            _ActiveColour = value;
        }
    }

    protected override void CreateHandle() {
        base.CreateHandle();
        Alignment = TabAlignment.Top;
    }

    private TabPage predraggedTab;

    protected override void OnMouseDown(MouseEventArgs e) {
        predraggedTab = getPointedTab();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        predraggedTab = null;
        base.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (e.Button == MouseButtons.Left && predraggedTab != null) {
            this.DoDragDrop(predraggedTab, DragDropEffects.Move);
        }
        base.OnMouseMove(e);
    }

    protected override void OnDragOver(DragEventArgs drgevent) {
        TabPage draggedTab = (TabPage)drgevent.Data.GetData(typeof(TabPage));
        TabPage pointedTab = getPointedTab();

        if (draggedTab == predraggedTab && pointedTab != null) {
            drgevent.Effect = DragDropEffects.Move;

            if (!(pointedTab == draggedTab)) {
                swapTabPages(draggedTab, pointedTab);
            }
        }

        base.OnDragOver(drgevent);
    }

    private TabPage getPointedTab() {
        for (int i = 0; i < this.TabPages.Count; i++) {
            if (this.GetTabRect(i).Contains(this.PointToClient(Cursor.Position))) {
                return this.TabPages[i];
            }
        }

        return null;
    }

    private void swapTabPages(TabPage src, TabPage dst) {
        int srci = this.TabPages.IndexOf(src);
        int dsti = this.TabPages.IndexOf(dst);

        this.TabPages[dsti] = src;
        this.TabPages[srci] = dst;

        if (this.SelectedIndex == srci) {
            this.SelectedIndex = dsti;
        } else if (this.SelectedIndex == dsti) {
            this.SelectedIndex = srci;
        }

        this.Refresh();
    }

    #endregion

    #region Draw Control

    public VSTabControl() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        DoubleBuffered = true;
        SizeMode = TabSizeMode.Normal;
        ItemSize = new Size(240, 16);
        AllowDrop = true;
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(_BaseColour);
        try {
            SelectedTab.BackColor = _BackTabColour;
        } catch (Exception ex) {
        }
        try {
            SelectedTab.BorderStyle = BorderStyle.None;
        } catch (Exception ex) {
        }
        for (var i = 0; i < TabCount; i++) {
            Rectangle Base = new Rectangle(new Point(GetTabRect(i).Location.X + 2, GetTabRect(i).Location.Y), new Size(GetTabRect(i).Width, GetTabRect(i).Height));
            Rectangle BaseSize = new Rectangle(Base.Location, new Size(Base.Width, Base.Height));
            if (i == SelectedIndex) {
                g.FillRectangle(new SolidBrush(_BaseColour), BaseSize);
                g.FillRectangle(new SolidBrush(_ActiveColour), new Rectangle(Base.X - 5, Base.Y - 3, Base.Width, Base.Height + 5));
                g.DrawString(TabPages[i].Text, Font, new SolidBrush(_TextColour), BaseSize, CenterSF);
            } else {
                g.FillRectangle(new SolidBrush(_BaseColour), BaseSize);
                g.DrawString(TabPages[i].Text, Font, new SolidBrush(_TextColour), BaseSize, CenterSF);
            }
        }
        g.DrawLine(new Pen(_HorizLineColour, 2F), new Point(0, 19), new Point(Width, 19));
        g.FillRectangle(new SolidBrush(_BackTabColour), new Rectangle(0, 20, Width, Height - 20));
        //g.DrawRectangle(new Pen(_BorderColour, 2F), new Rectangle(0, 0, Width, Height));
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }

    #endregion

}

public class VSNormalTextBox : Control {
    #region Declarations
    private MouseState State = MouseState.None;
    private System.Windows.Forms.TextBox TB;
    private Color _BaseColour = Color.FromArgb(51, 51, 55);
    private Color _TextColour = Color.FromArgb(153, 153, 153);
    private Color _BorderColour = Color.FromArgb(35, 35, 35);
    private Styles _Style = Styles.NotRounded;
    private HorizontalAlignment _TextAlign = HorizontalAlignment.Left;
    private int _MaxLength = 32767;
    private bool _ReadOnly;
    private bool _UseSystemPasswordChar;
    private bool _Multiline;
    #endregion

    #region TextBox Properties

    public enum Styles {
        Rounded,
        NotRounded
    }

    [Category("Options")]
    public HorizontalAlignment TextAlign {
        get {
            return _TextAlign;
        }
        set {
            _TextAlign = value;
            if (TB != null) {
                TB.TextAlign = value;
            }
        }
    }

    [Category("Options")]
    public int MaxLength {
        get {
            return _MaxLength;
        }
        set {
            _MaxLength = value;
            if (TB != null) {
                TB.MaxLength = value;
            }
        }
    }

    [Category("Options")]
    public bool ReadOnly {
        get {
            return _ReadOnly;
        }
        set {
            _ReadOnly = value;
            if (TB != null) {
                TB.ReadOnly = value;
            }
        }
    }

    [Category("Options")]
    public bool UseSystemPasswordChar {
        get {
            return _UseSystemPasswordChar;
        }
        set {
            _UseSystemPasswordChar = value;
            if (TB != null) {
                TB.UseSystemPasswordChar = value;
            }
        }
    }

    [Category("Options")]
    public bool Multiline {
        get {
            return _Multiline;
        }
        set {
            _Multiline = value;
            if (TB != null) {
                TB.Multiline = value;

                if (value) {
                    TB.Height = Height - 7;
                } else {
                    Height = TB.Height + 7;
                }

            }
        }
    }

    [Category("Options")]
    public override string Text {
        get {
            return base.Text;
        }
        set {
            base.Text = value;
            if (TB != null) {
                TB.Text = value;
            }
        }
    }

    [Category("Options")]
    public override Font Font {
        get {
            return base.Font;
        }
        set {
            base.Font = value;
            if (TB != null) {
                TB.Font = value;
                TB.Location = new Point(3, 5);
                TB.Width = Width - 6;

                if (!_Multiline) {
                    Height = TB.Height + 7;
                }
            }
        }
    }

    protected override void OnCreateControl() {
        base.OnCreateControl();
        if (!(Controls.Contains(TB))) {
            Controls.Add(TB);
        }
    }

    private void OnBaseTextChanged(object s, EventArgs e) {
        Text = TB.Text;
    }

    private void OnBaseKeyDown(object s, KeyEventArgs e) {
        if (e.Control && e.KeyCode == Keys.A) {
            TB.SelectAll();
            e.SuppressKeyPress = true;
        }
        if (e.Control && e.KeyCode == Keys.C) {
            TB.Copy();
            e.SuppressKeyPress = true;
        }
    }

    protected override void OnResize(EventArgs e) {
        TB.Location = new Point(5, 5);
        TB.Width = Width - 10;

        if (_Multiline) {
            TB.Height = Height - 7;
        } else {
            Height = TB.Height + 7;
        }

        base.OnResize(e);
    }

    public Styles Style {
        get {
            return _Style;
        }
        set {
            _Style = value;
            Invalidate();
        }
    }

    public void SelectAll() {
        TB.Focus();
        TB.SelectAll();
    }


    #endregion

    #region Colour Properties

    [Category("Colours")]
    public Color BackgroundColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color TextColour {
        get {
            return _TextColour;
        }
        set {
            _TextColour = value;
        }
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    #endregion

    #region Mouse States

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        State = MouseState.Down;
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        State = MouseState.Over;
        TB.Focus();
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e) {
        base.OnMouseLeave(e);
        State = MouseState.None;
        Invalidate();
    }

    #endregion

    #region Draw Control
    public VSNormalTextBox() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        TB = new System.Windows.Forms.TextBox();
        TB.Height = 20;
        TB.Font = new Font("Segoe UI", 10);
        TB.Text = Text;
        TB.BackColor = _BaseColour;
        TB.ForeColor = _TextColour;
        TB.MaxLength = _MaxLength;
        TB.Multiline = false;
        TB.ReadOnly = _ReadOnly;
        TB.UseSystemPasswordChar = _UseSystemPasswordChar;
        TB.BorderStyle = BorderStyle.None;
        TB.Location = new Point(5, 5);
        TB.Width = Width - 35;
        TB.TextChanged += OnBaseTextChanged;
        TB.KeyDown += OnBaseKeyDown;
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        GraphicsPath GP = null;
        Rectangle Base = new Rectangle(0, 0, Width, Height);
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(BackColor);
        TB.BackColor = _BaseColour;
        TB.ForeColor = _TextColour;
        switch (_Style) {
            case Styles.Rounded:
                GP = DrawHelpers.RoundRectangle(Base, 6);
                g.FillPath(new SolidBrush(Color.FromArgb(42, 42, 42)), GP);
                g.DrawPath(new Pen(new SolidBrush(Color.FromArgb(35, 35, 35)), 2F), GP);
                GP.Dispose();
                break;
            case Styles.NotRounded:
                g.FillRectangle(new SolidBrush(_BaseColour), new Rectangle(0, 0, Width - 1, Height - 1));
                g.DrawRectangle(new Pen(Color.FromArgb(63, 63, 70), 2F), new Rectangle(0, 0, Width, Height));
                break;
        }
        g.InterpolationMode = (InterpolationMode)7;
    }

    #endregion

}

public class VSGroupBox : ContainerControl {
    #region Declarations
    private Color _MainColour = Color.FromArgb(37, 37, 38);
    private Color _HeaderColour = Color.FromArgb(45, 45, 48);
    private Color _TextColour = Color.FromArgb(129, 129, 131);
    private Color _BorderColour = Color.FromArgb(2, 118, 196);
    #endregion

    #region Properties

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    [Category("Colours")]
    public Color TextColour {
        get {
            return _TextColour;
        }
        set {
            _TextColour = value;
        }
    }

    [Category("Colours")]
    public Color HeaderColour {
        get {
            return _HeaderColour;
        }
        set {
            _HeaderColour = value;
        }
    }

    [Category("Colours")]
    public Color MainColour {
        get {
            return _MainColour;
        }
        set {
            _MainColour = value;
        }
    }

    #endregion

    #region Draw Control
    public VSGroupBox() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        Size = new Size(160, 110);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.FillRectangle(new SolidBrush(_MainColour), new Rectangle(0, 28, Width, Height));
        g.FillRectangle(new SolidBrush(_HeaderColour), new Rectangle(0, 0, Width, 28));
        g.FillRectangle(new SolidBrush(Color.FromArgb(33, 33, 33)), new Rectangle(0, 28, Width, 1));
        g.DrawString(Text, Font, new SolidBrush(_TextColour), new Point(5, 5));
        g.DrawRectangle(new Pen(_BorderColour, 2F), new Rectangle(0, 0, Width, Height));
        g.InterpolationMode = (InterpolationMode)7;
    }
    #endregion

}


public class VSComboBox : ComboBox {
    #region Declarations
    private int _StartIndex = 0;
    private Color _BorderColour = Color.FromArgb(35, 35, 35);
    private Color _BaseColour = Color.FromArgb(51, 51, 55);
    private Color _FontColour = Color.FromArgb(255, 255, 255);
    private Color _LineColour = Color.FromArgb(0, 122, 204);
    private Color _SqaureColour = Color.FromArgb(51, 51, 55);
    private Color _ArrowColour = Color.FromArgb(153, 153, 153);
    private Color _SqaureHoverColour = Color.FromArgb(52, 52, 52);
    private MouseState State = MouseState.None;
    #endregion

    #region Properties & Events

    [Category("Colours")]
    public Color LineColour {
        get {
            return _LineColour;
        }
        set {
            _LineColour = value;
        }
    }

    [Category("Colours")]
    public Color SqaureColour {
        get {
            return _SqaureColour;
        }
        set {
            _SqaureColour = value;
        }
    }

    [Category("Colours")]
    public Color ArrowColour {
        get {
            return _ArrowColour;
        }
        set {
            _ArrowColour = value;
        }
    }

    [Category("Colours")]
    public Color SqaureHoverColour {
        get {
            return _SqaureHoverColour;
        }
        set {
            _SqaureHoverColour = value;
        }
    }

    protected override void OnMouseEnter(EventArgs e) {
        base.OnMouseEnter(e);
        State = MouseState.Over;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        base.OnMouseLeave(e);
        State = MouseState.None;
        Invalidate();
    }

    [Category("Colours")]
    public Color BorderColour {
        get {
            return _BorderColour;
        }
        set {
            _BorderColour = value;
        }
    }

    [Category("Colours")]
    public Color BaseColour {
        get {
            return _BaseColour;
        }
        set {
            _BaseColour = value;
        }
    }

    [Category("Colours")]
    public Color FontColour {
        get {
            return _FontColour;
        }
        set {
            _FontColour = value;
        }
    }

    public int StartIndex {
        get {
            return _StartIndex;
        }
        set {
            _StartIndex = value;
            try {
                base.SelectedIndex = value;
            } catch {
            }
            Invalidate();
        }
    }

    protected override void OnTextChanged(System.EventArgs e) {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        Invalidate();
        OnMouseClick(e);
    }

    protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e) {
        Invalidate();
        base.OnMouseUp(e);
    }

    #endregion

    #region Draw Control

    public void ReplaceItem(object sender, System.Windows.Forms.DrawItemEventArgs e) {
        e.DrawBackground();
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        Rectangle Rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width + 1, e.Bounds.Height + 1);
        try {
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) {
                e.Graphics.FillRectangle(new SolidBrush(_SqaureColour), Rect);
                e.Graphics.DrawString(base.GetItemText(base.Items[e.Index]), Font, new SolidBrush(_FontColour), 1, e.Bounds.Top + 2);
            } else {
                e.Graphics.FillRectangle(new SolidBrush(_BaseColour), Rect);
                e.Graphics.DrawString(base.GetItemText(base.Items[e.Index]), Font, new SolidBrush(_FontColour), 1, e.Bounds.Top + 2);
            }
        } catch {
        }
        e.DrawFocusRectangle();
        Invalidate();

    }

    public VSComboBox() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        Width = 163;
        Font = new Font("Segoe UI", 10);
        SubscribeToEvents();
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(BackColor);
        try {
            Rectangle Square = new Rectangle(Width - 25, 0, Width, Height);
            g.FillRectangle(new SolidBrush(_BaseColour), new Rectangle(0, 0, Width - 25, Height));
            
            if (State == MouseState.None) {
                g.FillRectangle(new SolidBrush(_SqaureColour), Square);
            }
            else if (State == MouseState.Over) {
                g.FillRectangle(new SolidBrush(_SqaureHoverColour), Square);
            }
            g.DrawLine(new Pen(_LineColour, 2), new Point(Width - 26, 1), new Point(Width - 26, Height - 1));
            try {
                g.DrawString(Text, Font, new SolidBrush(_FontColour), new Rectangle(3, 0, Width - 20, Height), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap });
            } catch {
            }
            g.DrawRectangle(new Pen(_BorderColour, 2), new Rectangle(0, 0, Width, Height));
            Point[] P = {
                new Point(Width - 17, 11),
                new Point(Width - 13, 5),
                new Point(Width - 9, 11)
            };
            g.FillPolygon(new SolidBrush(_ArrowColour), P);
            g.DrawPolygon(new Pen(_ArrowColour), P);
            Point[] P1 = {
                new Point(Width - 17, 15),
                new Point(Width - 13, 21),
                new Point(Width - 9, 15)
            };
            g.FillPolygon(new SolidBrush(_ArrowColour), P1);
            g.DrawPolygon(new Pen(_ArrowColour), P1);
            } catch {
            }
            g.InterpolationMode = (InterpolationMode)7;

        }

#endregion


    private bool EventsSubscribed = false;
    private void SubscribeToEvents() {
        if (EventsSubscribed)
            return;
        else
            EventsSubscribed = true;

        this.DrawItem += ReplaceItem;
    }

}