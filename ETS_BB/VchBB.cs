using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptSolution;
using ScriptSolution.Indicators;
using ScriptSolution.Model;

namespace ModulSolution.Robots
{
    public class CVchBB : Script
    {
        //------------------------------------------------------------------------------------------ 
        //  Вненшние парнаметры 
        //------------------------------------------------------------------------------------------ 
        public CreateInidicator  i_bbIn = new CreateInidicator(EnumIndicators.BollinderBands, 0, "Вход");
        public CreateInidicator  i_bbOut = new CreateInidicator(EnumIndicators.BollinderBands, 0, "Выход");
        public ParamOptimization i_deltaIn = new ParamOptimization(10000, 10000, 50000, 10000, "Отклонение на вход )",
        "Данная величина умножается на шаг цены.");
        public ParamOptimization i_deltaOut = new ParamOptimization(10000, 10000, 50000, 10000, "Отклонение на выход",
        "Данная величина умножается на шаг цены.");
        public ParamOptimization i_lot1 = new ParamOptimization(1, 0, 0, 0, "Объем входа 1-й части позиции",
        "Данная величина умножается на размер лота");
        public ParamOptimization i_lot2 = new ParamOptimization(1, 0, 0, 0, "Объем входа 2-й части позиции",
        "Данная величина умножается на размер лота");
        public ParamOptimization i_profit = new ParamOptimization(30000, 30000, 10000, 100000, "Профит для выхода из 2-й позиции",
        "Данная величина умножается на шаг цены");
        //------------------------------------------------------------------------------------------
        //  Добавление к LONG позиции
        //------------------------------------------------------------------------------------------
        private void AddLongPosition(int bar)
        {
            if (i_bbIn.param.LinesIndicators[0].LineParam[0].Value < bar)
            {
                var bbDown = i_bbIn.param.LinesIndicators[2].PriceSeries;
                double addPrice = Math.Round(bbDown[bar] - i_deltaIn.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);
                double lot = Math.Round(i_lot2.ValueInt * FinInfo.Security.LotSize, FinInfo.Security.LotSizeAccuracy);
                BuyAtLimit(bar + 1, addPrice, lot, "Добавить LONG");
            }
        }
        //------------------------------------------------------------------------------------------
        //  Добавление к SHORT позиции
        //------------------------------------------------------------------------------------------
        private void AddShortPosition(int bar)
        {
            if (i_bbIn.param.LinesIndicators[0].LineParam[0].Value < bar)
            {
                var bbUp = i_bbIn.param.LinesIndicators[1].PriceSeries;
                double addPrice = Math.Round(bbUp[bar] + i_deltaIn.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);
                double lot = Math.Round(i_lot2.ValueInt * FinInfo.Security.LotSize, FinInfo.Security.LotSizeAccuracy);
                ShortAtLimit(bar + 1, addPrice, lot, "Добавить SHORT");
            }
        }
        //------------------------------------------------------------------------------------------
        //  Закрытие LONG
        //------------------------------------------------------------------------------------------
        private void CheckForCloseLong(int bar)
        {
            if (i_bbOut.param.LinesIndicators[0].LineParam[0].Value < bar)
            {
                var bbUp = i_bbOut.param.LinesIndicators[1].PriceSeries;
                double outPrice = Math.Round(bbUp[bar] + i_deltaOut.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);

                for( int i=0; i<LongPos.Count; i++ )
                {
                    SellAtProfit(bar + 1, LongPos[i], outPrice, "Закрытие LONG по общим условиям");
                }
            }
            //--- Если позиция не закрылась по общим условиям, проверячем закрытие части по TP
            if(LongPos.Count == 2 )
            {
                double tp = Math.Round(LongPos[1].EntryPrice + i_profit.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);
                SellAtProfit(bar + 1, LongPos[1], tp, "Закрытие части LONG по TP");
            }
        }
        //------------------------------------------------------------------------------------------
        //  Закрытие SHORT 
        //------------------------------------------------------------------------------------------ 
        private void CheckForCloseShort( int bar )
        {
            if (i_bbOut.param.LinesIndicators[0].LineParam[0].Value < bar)
            {
                var bbDown = i_bbOut.param.LinesIndicators[2].PriceSeries;
                double outPrice = Math.Round(bbDown[bar] - i_deltaOut.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);

                for (int i = 0; i < ShortPos.Count; i++)
                {
                    CoverAtProfit(bar + 1, ShortPos[i], outPrice, "Закрытие SHORT по общим условиям");
                }
            }
            //--- Если позиция не закрылась по общим условиям, проверячем закрытие части по TP
            if (ShortPos.Count == 2)
            {
                double tp = Math.Round(ShortPos[1].EntryPrice - i_profit.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);
                CoverAtProfit(bar + 1, ShortPos[1], tp, "Закрытие части SHORT по TP");
            }
        }
        //------------------------------------------------------------------------------------------ 
        //  Открытие позиции
        //------------------------------------------------------------------------------------------ 
        private void CheckForOpen(int bar)
        {
            if (i_bbIn.param.LinesIndicators[0].LineParam[0].Value < bar)
            {
                var bbUp = i_bbIn.param.LinesIndicators[1].PriceSeries;
                var bbDown = i_bbIn.param.LinesIndicators[2].PriceSeries;
                double inPrice = Math.Round(bbDown[bar] - i_deltaIn.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy );
                double lot = Math.Round(i_lot1.ValueInt * FinInfo.Security.LotSize, FinInfo.Security.LotSizeAccuracy);
                BuyAtLimit(bar + 1, inPrice, lot, "Открытие LONG");
                
                if (LongPos.Count > 0)
                    return;

                inPrice = Math.Round(bbUp[bar] + i_deltaIn.ValueInt * FinInfo.Security.MinStep, FinInfo.Security.Accuracy);
                ShortAtLimit(bar + 1, inPrice, lot, "Отткрытие SHORT");
            }
        }
        //------------------------------------------------------------------------------------------ 
        //
        //------------------------------------------------------------------------------------------ 
        public override void Execute()
        {
            for (var bar = IndexBar; bar < CandleCount - 1; bar++)
            {
                //--- Проверка на закрытие SHORT
                if (ShortPos.Count > 0)
                    CheckForCloseShort(bar);
                //--- Проверка на закрытие LONG
                if (LongPos.Count > 0)
                    CheckForCloseLong(bar);
                //--- Проверка на открытие позиции 
                if (ShortPos.Count == 0 && LongPos.Count == 0 )
                    CheckForOpen(bar);
                //--- Проверка на добавление SHORT позиции
                if (ShortPos.Count == 1)
                    AddShortPosition(bar);
                //--- Проверка на добавление LONG позиции
                if (LongPos.Count == 1)
                    AddLongPosition(bar);
            }
        }

        public override void GetAttributesStratetgy()
        {
            DesParamStratetgy.Version = "1.0.0.0";
            DesParamStratetgy.DateRelease = "01.06.2022";
            DesParamStratetgy.DateChange = "01.06.2022";
            DesParamStratetgy.Author = "Vladimir Chernianskiy";
            DesParamStratetgy.Description = "BB";
            DesParamStratetgy.Change = "";
            DesParamStratetgy.NameStrategy = "Отбой от канала BB";
        }
    }
}
