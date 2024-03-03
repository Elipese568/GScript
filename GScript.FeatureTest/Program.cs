using System.Text;

namespace GScript.FeatureTest
{
    public class Number : IEquatable<Number?>
    {
        List<char> _chars;
        bool _isLessThanZero = false;
        int length = 0;

        public int Length { get { return length; } }

        public Number(string chars)
        {
            if (chars[0] == '-')
            {
                _isLessThanZero = true;
                this._chars = new(chars[1..].Reverse());
            }
            else
                this._chars = new(chars.Reverse());

            length = _chars.Count;
        }

        public Number(IEnumerable<char> chars, bool isLessThanZero)
        {
            this._chars = new(chars);
            _isLessThanZero = isLessThanZero;
            length = _chars.Count;
        }

        private void Update(List<char> chars)
        {
            _chars = chars;
            length = _chars.Count;
        }

        private static int ParseCharToInt(char c)
        {
            return c switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                _ => throw new FormatException($"Char '{c}' is invaild.")
            };
        }

        private static char ParseIntToChar(int c)
        {
            return c switch
            {
                0 => '0',
                1 => '1',
                2 => '2',
                3 => '3',
                4 => '4',
                5 => '5',
                6 => '6',
                7 => '7',
                8 => '8',
                9 => '9'
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Number);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            if (_isLessThanZero)
                sb.Append('-');

            sb.Append(_chars.Reverse<char>().ToArray());
            return sb.ToString();
        }

        public bool Equals(Number? other)
        {
            return other is not null &&
                   EqualityComparer<List<char>>.Default.Equals(_chars, other._chars) &&
                   _isLessThanZero == other._isLessThanZero;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_chars, _isLessThanZero);
        }

        public static Number operator +(Number number, Number numberb)
        {
            int overflow = 0;
            bool isLessThanZero = number._isLessThanZero ^ numberb._isLessThanZero;
            List<char> result = new();

            for (int i = 0; i < Math.Max(numberb._chars.Count, number._chars.Count); i++)
            {
                int adda = i < number._chars.Count ? ParseCharToInt(number._chars[i]) : 0;
                int addb = i < numberb._chars.Count ? ParseCharToInt(numberb._chars[i]) : 0;
                int current = adda + addb + overflow;
                overflow = 0;
                if (current >= 10)
                {
                    overflow = (current - current % 10) / 10;
                    current %= 10;
                }

                result.Add(ParseIntToChar(current));
            }

            var resultobject = new Number(result, isLessThanZero);

            return resultobject;
        }

        public static bool operator ==(Number? left, Number? right)
        {
            return EqualityComparer<Number>.Default.Equals(left, right);
        }

        public static bool operator !=(Number? left, Number? right)
        {
            return !(left == right);
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Number a = new("-114514");
            var b = a + new Number("-114514");
            Console.WriteLine(b);
        }
    }
}