using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    internal static class PlanTextView
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        internal static void Paint(RichTextBox box, PlanSnapshot plan)
        {
            int start = box.SelectionStart, length = box.SelectionLength;
            int line = (int)SendMessage(box.Handle, 0x00CE, IntPtr.Zero, IntPtr.Zero);
            if (box.Text != plan.Text)
                box.Text = plan.Text;
            box.SelectAll();
            box.SelectionBackColor = box.BackColor;
            foreach (var mark in plan.Marks)
                if (mark.Start >= 0 && mark.Length > 0 && mark.Start <= box.TextLength - mark.Length && box.Text.Substring(mark.Start, mark.Length) == mark.Text)
                {
                    box.Select(mark.Start, mark.Length);
                    box.SelectionBackColor = Color.LightGreen;
                }

            box.Select(Math.Min(start, box.TextLength), Math.Min(length, Math.Max(0, box.TextLength - start)));
            int now = (int)SendMessage(box.Handle, 0x00CE, IntPtr.Zero, IntPtr.Zero);
            SendMessage(box.Handle, 0x00B6, IntPtr.Zero, (IntPtr)(line - now));
        }
    }
}
