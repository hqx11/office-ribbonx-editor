﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OfficeRibbonXEditor.UITests.Extensions;

namespace OfficeRibbonXEditor.UITests.Main
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in TearDown")]
    public class MainWindowTests
    {
        private readonly string sourceFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources/Blank.xlsx");

        private readonly string destFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "Output/BlankSaved.xlsx");

        private readonly string exePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "OfficeRibbonXEditor.exe");

        private Application? app;

        private AutomationBase? automation;

        private Window? mainWindow;

        [TearDown]
        public void TearDown()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            if (this.mainWindow != null && status == TestStatus.Failed)
            {
                this.MakeMainWindowCapture("Main Window status when the test failed");
            }

            this.automation?.Dispose();

            // There are high chances that some unsaved changed warnings appear, so let's directly kill the application
            // If those warnings need to be tested, a specific test case can be created for them
            this.app?.Kill();
        }

        [Test]
        public void CanOpenWindow()
        {
            // Arrange / act
            this.LaunchApplication();

            // Assert
            Assert.AreEqual("Office RibbonX Editor", this.mainWindow?.Title);
        }

        [Test]
        public void CanOpenDocumentFromExecutable()
        {
            // Arrange / act
            this.LaunchApplication(sourceFile);
            var treeView = this.mainWindow!.FindTreeView();
            Assume.That(treeView, Is.Not.Null);

            // Assert
            Assert.AreEqual(1, treeView!.Items.Length);
            var item = treeView!.Items.First().AsDocumentItem();

            Assert.AreEqual(Path.GetFileName(sourceFile), item!.Title);
        }

        [Test]
        public void CanInsertSample()
        {
            // Arrange / act
            this.LaunchApplication(sourceFile);
            this.mainWindow?.FindTreeView()?.Items.First().Select();
            var menu = this.mainWindow?.FindFirstChild(cf => cf.Menu()).AsMenu();

            var sampleEntry = menu?.Items["Insert"].Items["Sample XML"].Items[0];
            sampleEntry?.Invoke();

            var tabItem = mainWindow?.FindTabView()?.SelectedTabItem;
            Assert.NotNull(tabItem);
        }

        private void LaunchApplication(params string[] arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Join(" ", arguments.Select(x => $"\"{x}\"")),
            };

            this.app = Application.Launch(psi);
            this.automation = new UIA3Automation();
            this.app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(5));
            this.mainWindow = app.GetAllTopLevelWindows(this.automation).First();
            Assert.NotNull(this.mainWindow, "Cannot find main window");
        }

        private void MakeMainWindowCapture(string description = "Main window status on TearDown")
        {
            if (this.mainWindow == null)
            {
                // Perhaps a call to LaunchApplication was missing in that particular test
                return;
            }

            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "MainWindow.png");

            var image = Capture.Element(this.mainWindow);
            image.ToFile(path);
            TestContext.AddTestAttachment(path, description);
        }
    }
}
