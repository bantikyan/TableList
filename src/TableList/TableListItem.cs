using System;
using System.Collections.Generic;
using System.Text;

namespace Zetalex.TableList
{
    [Serializable]
    public class TableListItem
    {
        public TableListItem()
        {
            TL_AllowModify = true;
            TL_AllowDelete = true;
        }

        [TableListHiddenInput]
        public TableListItemState TL_State { get; set; }
        [TableListHiddenInput]
        public bool TL_AllowModify { get; set; }
        [TableListHiddenInput]
        public bool TL_AllowDelete { get; set; }
    }
}
