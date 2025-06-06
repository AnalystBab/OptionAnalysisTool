using System;
using System.ComponentModel.DataAnnotations;
using OptionAnalysisTool.Models.KiteConnect;

namespace OptionAnalysisTool.Models
{
    public class Instrument
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExchangeToken { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TradingSymbol { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public decimal Strike { get; set; }
        public DateTime? Expiry { get; set; }
        
        [Required]
        [StringLength(10)]
        public string InstrumentType { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Segment { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Exchange { get; set; }

        public Instrument()
        {
            InstrumentToken = string.Empty;
            ExchangeToken = string.Empty;
            TradingSymbol = string.Empty;
            Name = string.Empty;
            InstrumentType = string.Empty;
            Segment = string.Empty;
            Exchange = string.Empty;
        }

        public static Instrument FromKiteInstrument(Instrument? kiteInstrument)
        {
            if (kiteInstrument is null)
            {
                return new Instrument();
            }

            var instrument = new Instrument
            {
                InstrumentToken = kiteInstrument.InstrumentToken.ToString(),
                ExchangeToken = kiteInstrument.ExchangeToken.ToString(),
                TradingSymbol = kiteInstrument.TradingSymbol,
                Name = kiteInstrument.Name,
                Strike = kiteInstrument.Strike,
                Expiry = kiteInstrument.Expiry,
                InstrumentType = kiteInstrument.InstrumentType,
                Segment = kiteInstrument.Segment,
                Exchange = kiteInstrument.Exchange
            };

            return instrument;
        }
    }
} 