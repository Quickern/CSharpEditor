/*
    CSharpEditor - A C# source code editor with syntax highlighting, intelligent
    code completion and real-time compilation error checking.
    Copyright (C) 2021  Giorgio Bianchini

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, version 3.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Avalonia.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpEditor
{
    internal class AutoSaver
    {
        public Editor Editor { get; }
        public EventWaitHandle ExitHandle { get; }
        public int MillisecondsInterval { get; set; } = 10000;
        public string AutoSaveFile { get; }

        private string _lastText;

        private readonly object StatusObject = new object();

        private Thread LoopThread;

        public bool IsRunning { get; private set; } = false;

        public static AutoSaver Start(Editor editor, string autoSaveFile)
        {
            AutoSaver saver = new AutoSaver(editor, autoSaveFile);

            editor.DetachedFromLogicalTree += (s, e) =>
            {
                saver.ExitHandle.Set();
            };

            return saver;
        }

        public void Resume()
        {
            lock (StatusObject)
            {
                ExitHandle.Reset();
                LoopThread.Join();
                LoopThread = new Thread(SaverLoop);
                LoopThread.Start();
            }
        }

        public void Stop()
        {
            lock (StatusObject)
            {
                ExitHandle.Set();
                LoopThread.Join();
            }
        }

        private AutoSaver(Editor editor, string autoSaveFile)
        {
            Editor = editor;
            _lastText = Editor.EditorControl.Text.ToString();

            ExitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            AutoSaveFile = autoSaveFile;
            LoopThread = new Thread(SaverLoop);
            LoopThread.Start();
        }

        private async void SaverLoop()
        {
            IsRunning = true;

            while (!ExitHandle.WaitOne(MillisecondsInterval))
            {
                await AutoSave();
            }

            IsRunning = false;
        }

        private async Task AutoSave()
        {
            if (Editor.AccessType == Editor.AccessTypes.ReadWrite)
            {
                string text = await Dispatcher.UIThread.InvokeAsync(() => Editor.EditorControl.Text.ToString());
                if (text == _lastText)
                    return;

                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AutoSaveFile));
                    System.IO.File.WriteAllText(AutoSaveFile, text);

                    _lastText = text;
                }
                catch { }


                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Editor.SaveHistoryContainer.IsVisible)
                    {
                        Editor.SaveHistoryContainer.Refresh();
                    }
                });

                Editor.InvokeAutosave(new SaveEventArgs(text));
            }
        }
    }
}
