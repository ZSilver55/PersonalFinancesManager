using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Enums
{
    public enum eIncomeType
    {
        // Not listed
        Other,

        // Earned Income
        Salary,
        Wages,
        Bonus,
        OvertimePay,
        Commission,
        Freelance,
        SelfEmployment,
        Tips,
        Honorarium,

        // Investment Income
        Dividends,
        Interest,
        CapitalGains,
        RentalIncome,
        REITDistribution,

        // Passive Income
        Royalties,
        AffiliateMarketing,
        CourseSales,
        LicensingRevenue,
        SilentPartnership,

        // Government and Social Support
        SocialSecurity,
        UnemploymentBenefits,
        DisabilityBenefits,
        VeteransBenefits,
        ChildSupport,
        Alimony,

        // Business or Miscellaneous Income
        BusinessProfits,
        TrustIncome,
        Annuity,
        GamblingWinnings,
        LegalSettlement,
        PrizeOrAward,
        Barter,

        // International / Other Income
        ForeignIncome,
        Remittance,
        CryptoIncome,
        Grants,
        Crowdfunding
    }
}
