// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting;

public sealed class Value
{
    public ValueKind Kind {get;}
    private readonly string _stringPayload = "";
    private readonly double _numberPayload;
    private readonly bool _booleanPayload;
    private readonly DateTime _dateTime;
    private readonly TimeSpan _timeSpan;

    public string AsString => _stringPayload;
    public double AsNumber => _numberPayload;
    public bool AsBool => _booleanPayload;
    public DateTime AsDateTime => _dateTime;
    public TimeSpan AsTime => _timeSpan;
    public bool IsTemporal => Kind is ValueKind.DateTime or ValueKind.Date or ValueKind.Time;

    private Value(ValueKind kind, string str, double num, bool b, DateTime dt, TimeSpan ts)
    {
        Kind = kind;
        _stringPayload = str;
        _numberPayload = num;
        _booleanPayload = b;
        _dateTime = dt;
        _timeSpan = ts;
    }

    public static Value Str(string s) => new(ValueKind.Str, s ?? "", 0, false, default, default);
    public static Value Num(double n) => new(ValueKind.Num, "", n, false, default, default);
    public static Value Bool(bool b) => new(ValueKind.Bool, "", 0, b, default, default);
    public static Value DateTimeVal(DateTime dt) => new(ValueKind.DateTime, "", 0, false, dt, default);
    public static Value DateVal(DateTime date) => new(ValueKind.Date, "", 0, false, date.Date, default);
    public static Value TimeVal(TimeSpan t) => new(ValueKind.Time, "", 0, false, default, t);

    public string Render() => Kind switch
    {
        ValueKind.Str => _stringPayload,
        ValueKind.Num => _numberPayload.ToString(CultureInfo.InvariantCulture),
        ValueKind.Bool => _booleanPayload ? "True" : "False",
        ValueKind.DateTime => _dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        ValueKind.Date => _dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ValueKind.Time => _timeSpan.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
        _ => ""
    };

    public string KindName => Kind switch
    {
        ValueKind.Str => "str",
        ValueKind.Num => "float",
        ValueKind.Bool => "bool",
        ValueKind.DateTime => "datetime",
        ValueKind.Date => "date",
        ValueKind.Time => "time",
        _ => "value"
    };
}