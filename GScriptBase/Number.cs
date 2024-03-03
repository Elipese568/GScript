using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EUtility.ValueEx;

namespace GScriptStandard;


/*public class Number : IEquatable<Number?>
{
    List<char> _chars;
    bool _isLessThanZero = false;
    public Number(string chars)
    {
        if (chars[0] == '-')
        {
            _isLessThanZero = true;
            this._chars = new(chars[1..].Reverse());
        }
        else
            this._chars = new(chars.Reverse());
    }

    public Number(IEnumerable<char> chars, bool isLessThanZero)
    {
        this._chars = new(chars);
        _isLessThanZero = isLessThanZero;
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

    public static Number operator +(Number number, int add)
    {
        int overflow = 0;
        bool isLessThanZero = add < 0 ^ number._isLessThanZero;
        List<char> result = new();

        var addstring = add.ToString().Reverse().ToArray();
        if (addstring[0] == '-')
            addstring = addstring[1..];

        for(int i = 0; i < Math.Max(addstring.Length, number._chars.Count); i++)
        {
            int adda = i < number._chars.Count ? ParseCharToInt(number._chars[i]) : 0;
            int addb = i < addstring.Length ? ParseCharToInt(addstring[i]) : 0;
            int current = adda + addb + overflow;
            overflow = 0;
            if(current >= 10)
            {
                overflow = (current - current % 10) / 10;
                current %= 10;
            }

            result.Add(ParseIntToChar(current));
        }
            
        var resultobject = new Number(result, isLessThanZero);

        return resultobject;
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
}*/

//public class Number
//{
//    List<long> _number;
//    bool _isLessThanZero;

//    public Number(string integer)
//    {
//        string afterint = "";
//        if (integer[0] == '-')
//        {
//            _isLessThanZero = true;
//            afterint = integer[1..];
//        }
//        else
//            afterint = integer;

//        foreach (var item in afterint.Chunk(9).Reverse())
//        {
//            _number.Add(long.Parse(new string(item)));
//        }
//    }

//    public static Number operator +(Number left, Number right)
//    {
        
//    }
//}
