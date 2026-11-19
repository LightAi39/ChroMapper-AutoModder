using System.IO;
using System.Text;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    // Unity discards the Console output, but the Parser and Analyzer report their errors there.
    // Every line written to the Console is sent to the Unity log instead.
    internal class UnityConsoleWriter : TextWriter
    {
        private readonly StringBuilder line = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (line)
            {
                if (value == '\n')
                {
                    Debug.Log("[Console] " + line.ToString().TrimEnd('\r'));
                    line.Clear();
                }
                else
                {
                    line.Append(value);
                }
            }
        }
    }
}
