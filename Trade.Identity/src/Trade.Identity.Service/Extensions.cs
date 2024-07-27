using Trade.Identity.Service.Dtos;
using Trade.Identity.Service.Entities;

namespace Trade.Identity.Service
{
    public static class Extensions
    {
        public static UserDto AsDto(this ApplicationUser user)
        {
            return new UserDto(
                user.Id, 
                user.UserName, 
                user.Email, 
                user.Gil,
                user.CreatedOn);
        }
    }
}
