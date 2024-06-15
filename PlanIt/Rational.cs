using System;
using System.Numerics;

namespace PlanIt
{
    struct Rational : IEquatable<Rational>, IComparable<Rational>
    {
        public readonly long Numerator;
        public readonly long Denominator;

        public static readonly Rational Zero = new Rational(0, 1);
        public static readonly Rational One = new Rational(1, 1);
        public static readonly Rational MinusOne = new Rational(-1, 1);
        public static readonly Rational MinValue = new Rational(int.MinValue, 1);
        public static readonly Rational MaxValue = new Rational(int.MaxValue, 1);


        public bool IsZero => Numerator == 0;
        public bool IsNegative => Sign() < 0;
        public bool IsPositive => Sign() > 0;

        public Rational(long value) : this(value, 1)
        {
        }

        public Rational(long numerator, long denominator)
        {
            if (numerator == 0)
            {
                denominator = 1;
            }
            else if (denominator == 0)
            {
                throw new ArgumentException("Denominator may not be zero", "denom");
            }
            else if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }

            long d = GCD(numerator, denominator);
            this.Numerator = numerator / d;
            this.Denominator = denominator / d;
        }

        private static long GCD(long x, long y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }

        private static long LCM(long x, long y)
        {
            return x / GCD(x, y) * y;
        }

        public Rational Abs()
        {
            return new Rational(Math.Abs(Numerator), Denominator);
        }

        public Rational Reciprocal()
        {
            return new Rational(Denominator, Numerator);
        }

        #region Conversion Operators

        public static implicit operator Rational(long i)
        {
            return new Rational(i, 1);
        }

        public static explicit operator double(Rational f)
        {
            return f.Numerator == 0 ? 0 : f.Numerator / (double)f.Denominator;
        }

        #endregion

        #region Arithmetic Operators

        public static Rational operator -(Rational f)
        {
            return new Rational(-f.Numerator, f.Denominator);
        }

        public static Rational operator +(Rational a, Rational b)
        {
            long m = LCM(a.Denominator, b.Denominator);
            long na = a.Numerator * m / a.Denominator;
            long nb = b.Numerator * m / b.Denominator;
            return new Rational(na + nb, m);
        }

        public static Rational operator -(Rational a, Rational b)
        {
            return a + (-b);
        }

        public static Rational operator *(Rational a, Rational b)
        {
            return new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        }

        public static Rational operator /(Rational a, Rational b)
        {
            return a * b.Reciprocal();
        }

        public static Rational operator %(Rational a, Rational b)
        {
            long l = a.Numerator * b.Denominator, r = a.Denominator * b.Numerator;
            long n = l / r;
            return new Rational(l - n * r, a.Denominator * b.Denominator);
        }

        #endregion

        #region Comparison Operators

        public static bool operator ==(Rational a, Rational b)
        {
            if (a.Numerator == 0) return b.Numerator == 0;
            return a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        }

        public static bool operator !=(Rational a, Rational b)
        {
            return !(a == b);
        }

        public static bool operator <(Rational a, Rational b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(Rational a, Rational b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(Rational a, Rational b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(Rational a, Rational b)
        {
            return a.CompareTo(b) >= 0;
        }

        #endregion

        #region Object Members

        public override bool Equals(object obj)
        {
            if (obj is Rational)
                return ((Rational)obj) == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Numerator.GetHashCode() ^ Denominator.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Numerator/(double)Denominator:0.##}";
//            return Num.ToString() + "/" + Denom.ToString();
        }

        #endregion

        #region IEquatable<Rational> Members

        public bool Equals(Rational other)
        {
            return other == this;
        }

        #endregion

        #region IComparable<Rational> Members

        public int CompareTo(Rational other)
        {
            var a = (double)this;
            var b = (double)other;
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
            //var a = Numerator * other.Denominator;
            //var b = other.Numerator * Denominator;
            //if (a < b) return -1;
            //if (a > b) return 1;
            //return 0;
            //var a = (double)this;
            //var b = (double)other;
            //if (a < b - double.Epsilon) return -1;
            //if (a > b + double.Epsilon) return 1;
            //return 0;
            //if (this == other)
            //{
            //    return 0;
            //}
            //if (Sign() < other.Sign())
            //{
            //    return -1;
            //}
            //if (Sign() > other.Sign())
            //{
            //    return 1;
            //}

            //if (Sign() > 0 && other.Sign() > 0)
            //{
            //    if (Numerator >= other.Numerator && Denominator <= other.Denominator)
            //    {
            //        return 1;
            //    }
            //    if (Numerator <= other.Numerator && Denominator >= other.Denominator)
            //    {
            //        return -1;
            //    }

            //    return Math.Sign(Numerator * other.Denominator - other.Numerator * Denominator);
            //}
            //else
            //{
            //    if (Math.Abs(Numerator) <= Math.Abs(other.Numerator) && Denominator >= other.Denominator)
            //    {
            //        return 1;
            //    }
            //    if (Math.Abs(Numerator) <= Math.Abs(other.Numerator) && Denominator <= other.Denominator)
            //    {
            //        return -1;
            //    }
            //    return Math.Sign(Math.Abs(other.Numerator) * Denominator - Math.Abs(Numerator) * other.Denominator);
            //}
        }

        private int Sign()
        {
            if (Denominator < 0)
            {
                if (Numerator < 0) return -1;
                if (Numerator > 0) return 1;
            }
            if (Numerator > 0) return 1;
            if (Numerator < 0) return -1;
            return 0;
        }

        #endregion
    }
}
