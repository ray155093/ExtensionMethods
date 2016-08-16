using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlerLibrary.DataModel
{
    public class BatchLogTodo
    {
        /// <summary>
        /// 批次序號
        /// </summary>
        public long BatchID { get; set; }
        /// <summary>
        /// 資料種類 0:基本資料1:營收資料
        /// </summary>
        public byte? DataType { get; set; }
        /// <summary>
        /// 營收資料資料時間
        /// </summary>
        public DateTime? DataDatetime { get; set; }
        /// <summary>
        /// 市場別序號
        /// </summary>
        public int? MarketTypeID { get; set; }

    }
}
