using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class FareDistanceSlabDto
    {
        public int? SlabId { get; set; }
        public decimal FromKm { get; set; }
        public decimal? ToKm { get; set; }
        public decimal RatePerKm { get; set; }
        public int SortOrder { get; set; }
    }

    public class FareSettingUpsertRequest
    {
        public int? SettingId { get; set; }
        public string? AreaType { get; set; }
        public decimal? BaseFare { get; set; }

        // Back-compat: if Slabs is null/empty, we will create one default slab using PerKmRate.
        public decimal? PerKmRate { get; set; }

        public decimal? PerMinuteRate { get; set; }
        public decimal? AdminPercentage { get; set; }

        public List<FareDistanceSlabDto>? Slabs { get; set; }
    }

    public class FareSettings
    {
        public int SettingId { get; set; }

        public string? AreaType { get; set; }

        public decimal? BaseFare { get; set; }

        public decimal? PerKmRate { get; set; }

        public decimal? PerMinuteRate { get; set; }

        public decimal? AdminPercentage { get; set; }

        public List<FareDistanceSlabDto>? Slabs { get; set; }
    }
}
