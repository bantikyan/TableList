using System;
using System.Collections.Generic;
using System.Text;

namespace Zetalex.TableList
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TableListHiddenInput : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TableListRadioButton : Attribute
    {
    }
}
