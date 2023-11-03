using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.Unity
{
    public struct Vector2 // literally straight from chatgpt i hope it works
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        public static Vector2 operator *(Vector2 a, float scalar)
        {
            return new Vector2(a.x * scalar, a.y * scalar);
        }

        public static Vector2 operator /(Vector2 a, float divisor)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException("Division by zero.");
            }
            return new Vector2(a.x / divisor, a.y / divisor);
        }

        public float magnitude
        {
            get { return (float)Math.Sqrt(x * x + y * y); }
        }

        public Vector2 normalized
        {
            get
            {
                float mag = magnitude;
                if (mag > 0)
                    return new Vector2(x / mag, y / mag);
                else
                    return zero;
            }
        }

        public float sqrMagnitude
        {
            get { return x * x + y * y; }
        }

        public static float Dot(Vector2 lhs, Vector2 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y;
        }

        public static float Distance(Vector2 a, Vector2 b)
        {
            float num = a.x - b.x;
            float num2 = a.y - b.y;
            return (float)Math.Sqrt(num * num + num2 * num2);
        }
    }

}
