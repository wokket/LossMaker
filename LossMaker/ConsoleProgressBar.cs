using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace LossMaker
{

    //translated from https://gist.github.com/vain0/8bba717af17525c2b06a46c73be2d06d

    public class ConsoleProgressBar : IDisposable, IProgress<double>
    {

        private const char Backspace = '\b';
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string _animation = "|/-\\";
        private int _animationIndex = 0;

        private double _currentProgress = 0.0;
        private string _currentText = String.Empty;

        private const int _blockCount = 10;
        private bool _disposed = false;
        private readonly Timer _timer;

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _currentProgress, value);
        }

        public void UpdateText(string text)
        {
            // Get length of common portion
            var commonPrefixLength = 0;
            var commonLength = Math.Min(_currentText.Length, text.Length);

            while (commonPrefixLength < commonLength &&
                    text[commonPrefixLength] == _currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            var outputBuilder = new StringBuilder();
            outputBuilder.Append(Backspace, _currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            var overlapCount = _currentText.Length - text.Length;

            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append(Backspace, overlapCount);
            }

            Console.Write(outputBuilder.ToString());
            _currentText = text;
        }



        public void ResetTimer()
        {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        private void TimerHandler(object state)
        {
            lock (_timer)
            {
                if (_disposed) { throw new ObjectDisposedException("ConsoleProgressBar"); }


                var progressBlockCount = Convert.ToInt32(_currentProgress * _blockCount);
                var percent = Convert.ToInt32(_currentProgress * 100.0);
                var text = String.Format(
                "[{0}{1}] {2,3}% {3}",
                new string('#', progressBlockCount),
                new string('-', _blockCount - progressBlockCount),
                percent,
                _animation[Interlocked.Increment(ref _animationIndex) % _animation.Length]
                );

                UpdateText(text);


                ResetTimer();
            }
        }

        public void Dispose()
        {

            lock (_timer)
            {
                _disposed = true;

                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();

                UpdateText(String.Empty);
            }
        }

        public ConsoleProgressBar()
        {
            _timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }



    }
}
