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

using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Shift + PrintScreen moves once from LastregionHotkey (its old default) to the silent full screen capture
    /// </summary>
    public class CoreConfigurationSilentCaptureTests
    {
        public CoreConfigurationSilentCaptureTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_LeaveShiftPrintScreenToTheSilentCapture()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();

            Assert.True(HotkeySequence.Parse(config.LastregionHotkey).IsEmpty);
            Assert.True(config.SilentCaptureHotkeyMoved);
        }

        [Theory]
        [InlineData("Shift + PrintScreen")]
        [InlineData("shift+printscreen")]
        public void OldDefault_IsClearedOnce(string oldHotkey)
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.LastregionHotkey = oldHotkey;
            config.SilentCaptureHotkeyMoved = false;

            config.OnAfterLoad();

            Assert.Equal("None", config.LastregionHotkey);
            Assert.True(config.SilentCaptureHotkeyMoved);

            // A later choice of the user stays
            config.LastregionHotkey = "Shift + PrintScreen";
            config.OnAfterLoad();
            Assert.Equal("Shift + PrintScreen", config.LastregionHotkey);
        }

        [Fact]
        public void AnotherHotkey_Stays()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.LastregionHotkey = "Ctrl + Shift + L";
            config.SilentCaptureHotkeyMoved = false;

            config.OnAfterLoad();

            Assert.Equal("Ctrl + Shift + L", config.LastregionHotkey);
        }
    }
}
