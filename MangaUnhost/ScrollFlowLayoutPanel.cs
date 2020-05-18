using System.Windows.Forms;

namespace MangaUnhost
{
    interface IMouseable {
        void DoMouseWhell(MouseEventArgs e);
    }
    class ScrollFlowLayoutPanel : FlowLayoutPanel, IMouseable
    {
        public void DoMouseWhell(MouseEventArgs e) => base.OnMouseWheel(e);
    }
}
