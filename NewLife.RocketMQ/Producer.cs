﻿using System;
using System.Linq;
using NewLife.RocketMQ.Client;
using NewLife.RocketMQ.Common;
using NewLife.RocketMQ.Protocol;

namespace NewLife.RocketMQ
{
    /// <summary>生产者</summary>
    public class Producer : MqBase
    {
        #region 属性
        //public Int32 DefaultTopicQueueNums { get; set; } = 4;

        //public Int32 SendMsgTimeout { get; set; } = 3_000;

        //public Int32 CompressMsgBodyOverHowmuch { get; set; } = 4096;

        //public Int32 RetryTimesWhenSendFailed { get; set; } = 2;

        //public Int32 RetryTimesWhenSendAsyncFailed { get; set; } = 2;

        //public Boolean RetryAnotherBrokerWhenNotStoreOK { get; set; }

        //public Int32 MaxMessageSize { get; set; } = 4 * 1024 * 1024;
        #endregion

        #region 基础方法
        //public override void Start()
        //{
        //    base.Start();
        //}

        #endregion

        #region 发送消息
        private static readonly DateTime _dt1970 = new DateTime(1970, 1, 1);
        /// <summary>发送消息</summary>
        /// <param name="msg"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public virtual SendResult Send(Message msg, Int32 timeout = -1)
        {
            var ts = DateTime.Now - _dt1970;
            var smrh = new SendMessageRequestHeader
            {
                ProducerGroup = Group,
                Topic = msg.Topic.IsNullOrEmpty() ? Topic : msg.Topic,
                QueueId = 0,
                SysFlag = 0,
                BornTimestamp = (Int64)ts.TotalMilliseconds,
                Flag = msg.Flag,
                Properties = msg.GetProperties(),
                ReconsumeTimes = 0,
                UnitMode = UnitMode,
            };

            var mq = SelectQueue(smrh.Topic);
            if (mq != null) smrh.QueueId = mq.QueueId;

            var dic = smrh.GetProperties();

            var bk = GetBroker(mq.BrokerName);

            var rs = bk.Invoke(RequestCode.SEND_MESSAGE_V2, msg.Body, dic);

            var sr = new SendResult
            {
                Status = SendStatus.SendOK,
                Queue = mq
            };
            sr.Read(rs.Header.ExtFields);

            return sr;
        }

        private WeightRoundRobin<BrokerInfo> _robin;
        //private Int32 _QueueIndex;
        /// <summary>选择队列</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        private MessageQueue SelectQueue(String topic)
        {
            if (_robin == null)
            {
                var list = Brokers.Where(e => e.Permission.HasFlag(Permissions.Write) && e.WriteQueueNums > 0).ToList();
                if (list.Count == 0) return null;

                var total = list.Sum(e => e.WriteQueueNums);
                if (total <= 0) return null;

                _robin = new WeightRoundRobin<BrokerInfo>(list, e => e.WriteQueueNums);
            }

            // 构造排序列表。希望能够均摊到各Broker
            var bk = _robin.Get(out var times);
            return new MessageQueue { Topic = Topic, BrokerName = bk.Name, QueueId = (times - 1) % bk.WriteQueueNums };

            //var idx = Interlocked.Increment(ref _QueueIndex);
            //idx = (idx - 1) % total;

            //// 轮询使用
            //foreach (var item in list)
            //{
            //    if (idx < item.WriteQueueNums)
            //        return new MessageQueue { Topic = Topic, BrokerName = item.Name, QueueId = idx };

            //    idx -= item.WriteQueueNums;
            //}

            //return null;
        }
        #endregion

        #region 连接池
        #endregion

        #region 业务方法
        /// <summary>创建主题</summary>
        /// <param name="key"></param>
        /// <param name="newTopic"></param>
        /// <param name="queueNum"></param>
        /// <param name="topicSysFlag"></param>
        public void CreateTopic(String key, String newTopic, Int32 queueNum, Int32 topicSysFlag = 0)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}