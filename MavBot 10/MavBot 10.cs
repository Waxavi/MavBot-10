using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MavBot10 : Robot
    {
        [Parameter("Lot Size", DefaultValue = 0.01)]
        public double _LotSize { get; set; }
        [Parameter("SL", DefaultValue = 10)]
        public double _SL { get; set; }
        [Parameter("TP", DefaultValue = 10)]
        public double _TP { get; set; }
        //--
        //[Parameter("Bollinger Bands", DefaultValue = "--------------------")]
        //public string _BollingerBands { get; set; }
        //[Parameter("BB Periods", DefaultValue = 20)]
        //public int _BBPeriods { get; set; }
        //[Parameter("BB Standard Deviation", DefaultValue = 2)]
        //public double _BBStd { get; set; }
        //[Parameter("BB Moving Avg Type")]
        //public MovingAverageType _BBMAT { get; set; }
        ////--
        //[Parameter("Keltner Channels", DefaultValue = "--------------------")]
        //public string _KeltnerChannels { get; set; }
        //[Parameter("_KC Periods", DefaultValue = 10)]
        //public int _KCPeriods { get; set; }
        //[Parameter("_KC Moving Avg Type")]
        //public MovingAverageType _KCMAT { get; set; }
        //[Parameter("_KC ATR Period", DefaultValue = 10)]
        //public int _KCATRPeriod { get; set; }
        //[Parameter("KC ATR Moving Avg Type")]
        //public MovingAverageType _KCATRMAT { get; set; }
        //[Parameter("KC ATR Band Distance", DefaultValue = 10)]
        //public double _KCBandDistance { get; set; }
        //--
        [Parameter("Stochastics", DefaultValue = "-----------------")]
        public string _Stochastics { get; set; }
        [Parameter("SO K Periods", DefaultValue = 10)]
        public int _SOKPeriods { get; set; }
        [Parameter("SO K Slowing", DefaultValue = 10)]
        public int _SOKSlowing { get; set; }
        [Parameter("SO D Periods", DefaultValue = 10)]
        public int _SODPeriods { get; set; }
        [Parameter("SO Moving Avg Type")]
        public MovingAverageType _SOMAT { get; set; }

        //--
        //[Parameter("SL/TP Stochastics", DefaultValue = "--------------------")]
        //public string _SLTPStochastics { get; set; }
        //[Parameter("SLTP SO K Periods", DefaultValue = 10)]
        //public int _SLTPSOKPeriods { get; set; }
        //[Parameter("SO K Slowing", DefaultValue = 10)]
        //public int _SLTPSOKSlowing { get; set; }
        //[Parameter("SO D Periods", DefaultValue = 10)]
        //public int _SLTPSODPeriods { get; set; }
        //[Parameter("SO Moving Avg Type")]
        //public MovingAverageType _SLTPSOMAT { get; set; }

        //--
        [Parameter("Squeeze Break", DefaultValue = "-----------------")]
        public string _SqueezeBreak { get; set; }
        [Parameter("Bollinger Period", DefaultValue = 20)]
        public int _SBBPeriod { get; set; }
        [Parameter("Bollinger Dev", DefaultValue = 2)]
        public double _SBBDev { get; set; }
        [Parameter("Keltner Periods", DefaultValue = 20)]
        public int _SBKPeriod { get; set; }
        [Parameter("Keltner Multiplier", DefaultValue = 1.5)]
        public double _SBKMult { get; set; }
        [Parameter("Momentum Period", DefaultValue = 12)]
        public int _SBMomentumP { get; set; }
        [Parameter("Listening Range", DefaultValue = 0)]
        public double _SBListeningRange { get; set; }
        //[Parameter("Momentum Period", DefaultValue = 12)]
        //public int _SBMomentum { get; set; }
        //--
        [Parameter("Anti Martingale", DefaultValue = "-----------------")]
        public string _AntiMartingale { get; set; }
        [Parameter("Anti-Martingale Switch", DefaultValue = false)]
        public bool _AntiMartingaleSwitch { get; set; }
        [Parameter("Anti-Martingale Multiplier", DefaultValue = 2)]
        public double _AntiMartingaleMultiplier { get; set; }
        [Parameter("Max Anti-Martingale LotSize", DefaultValue = 10)]
        public double _MaxAntiMartingaleLotSize { get; set; }


        //private BollingerBands _BB;
        //private KeltnerChannels _KC;
        private StochasticOscillator _SO, _SLTPSO;
        private SqueezeBreak _SB;
        private double _lots;

        private string _Label;

        private bool ZeroPos
        {
            get { return Positions.Count(item => item.Label == _Label) == 0; }
        }

        private bool AnyLongPos
        {
            get { return Positions.Count(item => item.Label == _Label && item.TradeType == TradeType.Buy) > 0; }
        }

        private bool AnyShortPos
        {
            get { return Positions.Count(item => item.Label == _Label && item.TradeType == TradeType.Sell) > 0; }
        }

        private DateTime SignalStart
        {
            get
            {
                int i = 0;

                while (_SB.Down.Last(i) < 0)
                {
                    i++;
                }

                return MarketSeries.OpenTime.Last(i);
            }
        }

        private DateTime LastTradeClosingTime
        {
            get
            {
                if (History.Count(item => item.Label == _Label) == 0)
                    return DateTime.MinValue;
                else
                    return History.Max(item => item.ClosingTime);
            }
        }

        private void MarketOrder(TradeType _tt)
        {
            TradeResult _TR = ExecuteMarketOrder(_tt, Symbol, Symbol.QuantityToVolume(_lots), _Label, _SL, _TP);
            if (_TR.IsSuccessful)
            {
                Print("Market Order Placed.");
            }
            else
            {
                Print("Error: {0}", _TR.Error);
            }
        }

        private void OnPositionsOpened(PositionOpenedEventArgs args)
        {
            if (args.Position.Label == _Label)
            {
                if (_AntiMartingaleSwitch)
                {

                }
            }
        }

        private void OnPositionsClosed(PositionClosedEventArgs args)
        {
            if (args.Position.Label == _Label)
            {
                if (_AntiMartingaleSwitch)
                {
                    if (args.Position.NetProfit > 0 && _lots * _AntiMartingaleMultiplier < _MaxAntiMartingaleLotSize)
                    {
                        _lots = _lots * _AntiMartingaleMultiplier;
                    }
                    else
                    {
                        _lots = _LotSize;
                    }
                }
            }
        }

        protected override void OnStart()
        {
            _lots = _LotSize;
            Positions.Opened += OnPositionsOpened;
            Positions.Closed += OnPositionsClosed;

            _Label = Symbol.Code + TimeFrame.ToString() + Server.Time.Ticks.ToString();
            //_BB = Indicators.BollingerBands(MarketSeries.Close, _BBPeriods, _BBStd, _BBMAT);
            //_KC = Indicators.KeltnerChannels(_KCPeriods, _KCMAT, _KCATRPeriod, _KCATRMAT, _KCBandDistance);
            _SO = Indicators.StochasticOscillator(_SOKPeriods, _SOKSlowing, _SODPeriods, _SOMAT);
            //_SLTPSO = Indicators.StochasticOscillator(_SLTPSOKPeriods, _SLTPSOKSlowing, _SLTPSODPeriods, _SLTPSOMAT);
            _SB = Indicators.GetIndicator<SqueezeBreak>(_SBBPeriod, _SBBDev, _SBKPeriod, _SBKMult, _SBMomentumP);
        }

        protected override void OnTick()
        {
            //Print("GitHub Changes.");

        }

        protected override void OnBar()
        {
            if (ZeroPos && SignalStart > LastTradeClosingTime)
            {
                if (_SB.Down.LastValue < _SBListeningRange * -1)
                {
                    if (_SO.PercentK.LastValue < 20)
                    {
                        MarketOrder(TradeType.Buy);
                    }
                    else if (_SO.PercentK.LastValue > 80)
                    {
                        MarketOrder(TradeType.Sell);
                    }
                }
            }
            //else
            //{
            //    if (AnyLongPos)
            //    {
            //        if (_SLTPSO.PercentK.LastValue > 80)
            //        {
            //            var _positions = Positions.Where(item => item.Label == _Label && item.TradeType == TradeType.Buy);
            //            if (_positions.Count() > 0)
            //                foreach (var pos in _positions)
            //                {
            //                    ClosePosition(pos);
            //                }
            //        }
            //    }
            //    else if (AnyShortPos)
            //    {
            //        if (_SLTPSO.PercentK.LastValue < 20)
            //        {
            //            var _positions = Positions.Where(item => item.Label == _Label && item.TradeType == TradeType.Sell);
            //            if (_positions.Count() > 0)
            //                foreach (var pos in _positions)
            //                {
            //                    ClosePosition(pos);
            //                }
            //        }
            //    }
            //}
        }

        protected override void OnStop()
        {
            if (IsBacktesting)
            {
                if (Positions.Count > 0)
                {
                    foreach (var pos in Positions)
                    {
                        ClosePosition(pos);
                    }
                }
            }
        }
    }
}
