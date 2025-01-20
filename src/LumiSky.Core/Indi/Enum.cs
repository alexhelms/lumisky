using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi;

public enum PropertyState
{
    Idle,
    Ok,
    Busy,
    Alert,
}

public enum SwitchState
{
    Off,
    On,
}

public enum SwitchRule
{
    OneOfMany,
    AtMostOne,
    AnyOfMany,
}

public enum PropertyPermission
{
    [XmlEnum("ro")]
    RO,
    [XmlEnum("wo")]
    WO,
    [XmlEnum("rw")]
    RW,
}

public enum BlobEnable
{
    Never,
    Also,
    Only,
}