/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.User32.Enums;

namespace Greenshot.Capturing
{
    /// <summary>
    /// A faint white flash over a display, shown after a silent capture: it never takes the focus, lets every click
    /// through, is not in Alt+Tab or the taskbar and fades out on its own. One per display, so a display with another DPI
    /// doesn't resize it.
    /// </summary>
    public sealed class CaptureFlashForm : Form
    {
        /// <summary>
        /// How white the flash starts, 0 to 1: faint, the screen stays readable
        /// </summary>
        internal const double StartOpacity = 0.35;

        /// <summary>
        /// How long the fade takes
        /// </summary>
        internal static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(320);

        /// <summary>
        /// The time between two steps of the fade (about 60 frames a second)
        /// </summary>
        private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(16);

        private CaptureFlashForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.White;
            Opacity = StartOpacity;
            TopMost = true;
            Bounds = bounds;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= (int)(ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW | ExtendedWindowStyleFlags.WS_EX_NOACTIVATE
                                              | ExtendedWindowStyleFlags.WS_EX_TRANSPARENT | ExtendedWindowStyleFlags.WS_EX_LAYERED
                                              | ExtendedWindowStyleFlags.WS_EX_TOPMOST);
                return createParams;
            }
        }

        /// <summary>
        /// The opacity of the flash after the time, it goes down from StartOpacity to 0 in FadeDuration
        /// </summary>
        internal static double OpacityAt(TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero) return StartOpacity;
            if (elapsed >= FadeDuration) return 0;
            double left = 1 - elapsed.TotalMilliseconds / FadeDuration.TotalMilliseconds;
            // Ease out: quick at first, gentle at the end
            return StartOpacity * left * left;
        }

        /// <summary>
        /// The displays to flash: those which overlap the captured area, all of them when it is unknown
        /// </summary>
        internal static IReadOnlyList<Rectangle> DisplaysToFlash(Rectangle capturedArea, IEnumerable<Rectangle> displays)
        {
            var all = displays.ToList();
            if (capturedArea.Width <= 0 || capturedArea.Height <= 0) return all;
            var overlapping = all.Where(d => d.IntersectsWith(capturedArea)).ToList();
            return overlapping.Count > 0 ? overlapping : all;
        }

        /// <summary>
        /// Flash the displays and fade out. Call it on the UI thread, it continues there (Task.Delay resumes on the
        /// synchronization context of the UI thread). With the animations of Windows off, the flash is shown without the
        /// fade and closed after the same time.
        /// </summary>
        /// <param name="capturedArea">The captured area in screen coordinates, empty for all displays</param>
        /// <param name="animate">False when Windows says no animations (or a remote session)</param>
        public static async Task ShowAsync(Rectangle capturedArea, bool animate)
        {
            var forms = DisplaysToFlash(capturedArea, Screen.AllScreens.Select(s => s.Bounds))
                .Select(bounds => new CaptureFlashForm(bounds)).ToList();
            try
            {
                foreach (var form in forms)
                {
                    form.Show();
                }

                if (!animate)
                {
                    await Task.Delay(FadeDuration).ConfigureAwait(true);
                    return;
                }

                var start = DateTime.UtcNow;
                while (true)
                {
                    await Task.Delay(FrameTime).ConfigureAwait(true);
                    double opacity = OpacityAt(DateTime.UtcNow - start);
                    if (opacity <= 0.01) break;
                    foreach (var form in forms)
                    {
                        form.Opacity = opacity;
                    }
                }
            }
            finally
            {
                foreach (var form in forms)
                {
                    form.Close();
                    form.Dispose();
                }
            }
        }
    }
}
