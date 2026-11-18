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

using System.Drawing;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Capturing;
using Greenshot.Recipes;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Shift + PrintScreen: all displays to a fixed folder, nothing asked, no notification, a faint flash
    /// </summary>
    public class SilentFullScreenRecipeTests
    {
        public SilentFullScreenRecipeTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CaptureRecipe Recipe() => new RecipeManager().GetRecipeById(RecipeManager.RecipeIdSilentFullScreen);

        [Fact]
        public void IsABuiltInRecipe_StartedByShiftPrintScreen()
        {
            var recipe = Recipe();

            Assert.NotNull(recipe);
            Assert.True(recipe.IsBuiltIn);
            var trigger = Assert.Single(recipe.Triggers, t => t.TriggerType == TriggerConfig.TypeHotkey);
            Assert.True(HotkeySequence.Parse("Shift + PrintScreen").Equals(HotkeySequence.Parse(trigger.GetHotkey(recipe))));
            Assert.Contains(new RecipeManager().GetActiveHotkeys(), h => h.Recipe.Id == RecipeManager.RecipeIdSilentFullScreen && h.Option != null);
        }

        [Fact]
        public void CapturesAllDisplays_WithoutWaiting()
        {
            var source = Recipe().Nodes.Single(n => n.StepType == WellKnownStepTypes.Source);

            Assert.Equal(CaptureSourceType.FullScreen, source.GetParameter<CaptureSourceType>("SourceType"));
            Assert.Equal(ScreenCaptureMode.FullScreen, source.GetParameter<ScreenCaptureMode>("ScreenCaptureMode"));
            Assert.Equal(0, source.GetParameter<int>("DelayMs", -1));
        }

        [Fact]
        public void SavesToTheFolder_WithoutAskingAnything_AndWithoutANotification()
        {
            var recipe = Recipe();
            var save = recipe.Nodes.Single(n => n.StepType == WellKnownStepTypes.SaveFile);

            Assert.Equal("${option.folder}", save.GetParameter<string>("SaveDirectory"));
            Assert.Equal("png", save.GetParameter<string>("Format"));
            Assert.Equal(FileExistsAction.AddNumber, save.GetParameter<FileExistsAction?>("IfFileExists"));
            Assert.False(save.GetParameter<bool>("PromptQuality", true));
            Assert.False(save.GetParameter<bool>("AskForFilename", true));
            Assert.DoesNotContain(recipe.Nodes, n => n.StepType == WellKnownStepTypes.Notification || n.StepType == WellKnownStepTypes.Destinations
                                                     || n.StepType == WellKnownStepTypes.Editor || n.StepType == WellKnownStepTypes.UserPrompt);

            var folder = recipe.FindOption(RecipeManager.SilentFolderOptionKey);
            Assert.NotNull(folder);
            Assert.True(folder.TryConvert(folder.DefaultValue, out var path), "The default folder must be a full path");
            Assert.EndsWith(@"\Greenshot", (string)path);
        }

        [Fact]
        public void Feedback_IsAFlash_WithoutSound()
        {
            var feedback = Recipe().Nodes.Single(n => n.StepType == WellKnownStepTypes.ImmediateFeedback);

            Assert.True(feedback.GetParameter<bool>(ImmediateFeedbackStep.FlashParameter));
            Assert.False(feedback.GetParameter<bool>("PlaySound", true));
        }

        [Fact]
        public void IsValid()
        {
            var result = RecipeValidator.Validate(Recipe());

            Assert.True(result.Errors.Count == 0, string.Join(" | ", result.Errors));
        }

        [Fact]
        public void Flash_FadesFromFaintToNothing()
        {
            Assert.True(CaptureFlashForm.StartOpacity > 0 && CaptureFlashForm.StartOpacity <= 0.5, "The flash is faint");
            Assert.Equal(CaptureFlashForm.StartOpacity, CaptureFlashForm.OpacityAt(System.TimeSpan.Zero));
            double half = CaptureFlashForm.OpacityAt(System.TimeSpan.FromTicks(CaptureFlashForm.FadeDuration.Ticks / 2));
            Assert.InRange(half, 0.001, CaptureFlashForm.StartOpacity);
            Assert.Equal(0, CaptureFlashForm.OpacityAt(CaptureFlashForm.FadeDuration));
        }

        [Fact]
        public void Flash_CoversTheCapturedDisplays()
        {
            var left = new Rectangle(-1920, 0, 1920, 1080);
            var main = new Rectangle(0, 0, 2560, 1440);
            var displays = new[] { left, main };

            Assert.Equal(displays, CaptureFlashForm.DisplaysToFlash(new Rectangle(-1920, 0, 4480, 1440), displays));
            Assert.Equal(new[] { main }, CaptureFlashForm.DisplaysToFlash(main, displays));
            Assert.Equal(displays, CaptureFlashForm.DisplaysToFlash(Rectangle.Empty, displays));
        }
    }
}
