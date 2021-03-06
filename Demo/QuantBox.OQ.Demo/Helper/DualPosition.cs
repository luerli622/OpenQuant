﻿using OpenQuant.API;
using QuantBox.OQ.Extensions;
using System.Collections.Generic;

namespace QuantBox.OQ.Demo.Helper
{
    public class DualPosition
    {
        public PositionRecord Long = new PositionRecord();
        public PositionRecord Short = new PositionRecord();

        public OrderBook_OneSide_Order Buy = new OrderBook_OneSide_Order(OrderSide.Buy);
        public OrderBook_OneSide_Order Sell = new OrderBook_OneSide_Order(OrderSide.Sell);

        public Dictionary<Order, EnumOpenClose> Order_OpenClose = new Dictionary<Order, EnumOpenClose>();

        public double NetQty
        {
            get { return Long.Qty - Short.Qty; }
        }

        public bool IsPending
        {
            get { return Buy.IsPending || Sell.IsPending; }
        }

        public void NewOrder(Order order)
        {
            lock (this)
            {
                if (!order.IsPendingNew)
                    return;

                EnumOpenClose OpenClose = OpenCloseHelper.CheckOpenClose(order);

                if (EnumOpenClose.OPEN == OpenClose)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Long.FrozenOpen += order.Qty;
                    }
                    else
                    {
                        Short.FrozenOpen += order.Qty;
                    }
                }
                else
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Short.FrozenClose += order.Qty;
                    }
                    else
                    {
                        Long.FrozenClose += order.Qty;
                    }
                }
                if (order.Side == OrderSide.Buy)
                {
                    Buy.Add(order);
                }
                else
                {
                    Sell.Add(order);
                }
                Order_OpenClose[order] = OpenClose;
            }
        }

        public void Filled(Order order)
        {
            lock (this)
            {
                EnumOpenClose OpenClose = EnumOpenClose.OPEN;
                Order_OpenClose.TryGetValue(order, out OpenClose);

                if (EnumOpenClose.OPEN == OpenClose)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Long.Qty += order.LastQty;
                        Long.FrozenOpen -= order.LastQty;
                        Long.CumOpenQty += order.LastQty;
                        Long.HoldingCost += order.LastPrice * order.LastQty;
                    }
                    else
                    {
                        Short.Qty += order.LastQty;
                        Short.FrozenOpen -= order.LastQty;
                        Short.CumOpenQty += order.LastQty;
                        Short.HoldingCost += order.LastPrice * order.LastQty;
                    }
                }
                else
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Short.Qty -= order.LastQty;
                        Short.FrozenClose -= order.LastQty;
                        if (Short.Qty == 0)
                        {
                            Short.HoldingCost = 0;
                        }
                        else
                        {
                            Short.HoldingCost -= order.LastPrice * order.LastQty;
                        }
                    }
                    else
                    {
                        Long.Qty -= order.LastQty;
                        Long.FrozenClose -= order.LastQty;
                        if (Long.Qty == 0)
                        {
                            Long.HoldingCost = 0;
                        }
                        else
                        {
                            Long.HoldingCost -= order.LastPrice * order.LastQty;
                        }
                    }
                }


                if (order.IsDone)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Buy.Remove(order);
                    }
                    else
                    {
                        Sell.Remove(order);
                    }
                }
                Order_OpenClose.Remove(order);
            }
        }

        public EnumOpenClose OrderCancelled(Order order)
        {
            lock (this)
            {
                EnumOpenClose OpenClose = EnumOpenClose.OPEN;
                Order_OpenClose.TryGetValue(order, out OpenClose);

                if (PositionSide.Long == OpenCloseHelper.CheckLongShort(order,OpenClose))
                {
                    ++Long.CumCancelCnt;
                }
                else
                {
                    ++Short.CumCancelCnt;
                }

                OrderRejected(order);

                return OpenClose;
            }
        }

        public EnumOpenClose GetOpenClose(Order order)
        {
            EnumOpenClose OpenClose = EnumOpenClose.OPEN;
            Order_OpenClose.TryGetValue(order, out OpenClose);
            return OpenClose;
        }

        public EnumOpenClose OrderRejected(Order order)
        {
            lock (this)
            {
                EnumOpenClose OpenClose = EnumOpenClose.OPEN;
                Order_OpenClose.TryGetValue(order, out OpenClose);

                if (EnumOpenClose.OPEN == OpenClose)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Long.FrozenOpen -= order.LeavesQty;
                    }
                    else
                    {
                        Short.FrozenOpen -= order.LeavesQty;
                    }
                }
                else
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Short.FrozenClose -= order.LeavesQty;
                    }
                    else
                    {
                        Long.FrozenClose -= order.LeavesQty;
                    }
                }

                if (order.IsDone)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        Buy.Remove(order);
                    }
                    else
                    {
                        Sell.Remove(order);
                    }
                }
                Order_OpenClose.Remove(order);

                return OpenClose;
            }
        }

        public EnumOpenClose CanClose(OrderSide Side, double qty)
        {
            if (CanCloseQty(Side) >= qty)
            {
                return EnumOpenClose.CLOSE;
            }
            else
            {
                return EnumOpenClose.OPEN;
            }
        }

        public double CanCloseQty(OrderSide Side)
        {
            double qty;
            if (Side == OrderSide.Buy)
            {
                qty = Short.CanCloseQty();
            }
            else
            {
                qty = Long.CanCloseQty();
            }
            return qty;
        }

        public override string ToString()
        {
            return Long + " --- " + Short;
        }
    }
}
