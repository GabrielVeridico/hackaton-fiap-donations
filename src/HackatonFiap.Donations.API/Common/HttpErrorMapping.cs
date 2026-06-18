using HackatonFiap.Donations.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Donations.API.Common;

public static class HttpErrorMapping
{
    public static IActionResult ToProblem(this Error error)
    {
        var status = error.Code switch
        {
            "Campaign.NotFound" => StatusCodes.Status404NotFound,
            "Donation.NotFound" => StatusCodes.Status404NotFound,
            "Campaign.InvalidStatusTransition" => StatusCodes.Status409Conflict,
            "Donation.CampaignNotFound" => StatusCodes.Status422UnprocessableEntity,
            "Donation.CampaignNotActive" => StatusCodes.Status422UnprocessableEntity,
            "Donation.OutsidePeriod" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest // validações (goal, datas, título, amount, período VO)
        };

        return new ObjectResult(new { error = error.Code, message = error.Message }) { StatusCode = status };
    }
}
