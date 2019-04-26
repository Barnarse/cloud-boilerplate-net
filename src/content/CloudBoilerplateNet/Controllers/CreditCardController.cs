using System.Threading.Tasks;

using CloudBoilerplateNet.Models;
using KenticoCloud.Delivery;
using Microsoft.AspNetCore.Mvc;

namespace CloudBoilerplateNet.Controllers
{
    public class CreditCardController : BaseController
    {
        public CreditCardController(IDeliveryClient deliveryClient) : base(deliveryClient)
        {
            
        }

        [Route("credit-cards")]
         public async Task<ViewResult>  Index()
        {
            // Get all credit cards
            var response = await DeliveryClient.GetItemsAsync<CreditCard>(
                new EqualsFilter("system.type", "credit_card"),
                new LimitParameter(3),
                new DepthParameter(1),
                new OrderParameter("elements.display_order")
            );
            
            // TODO: Convert to a view model here
            return View(response.Items);
        }
    }
}
