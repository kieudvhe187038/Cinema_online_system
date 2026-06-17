using Cinema_System.Application.DTOs;
using Cinema_System.Application.Common;

namespace Cinema_System.Application.ViewModels;

public class MovieDetailsViewModel
{
    public MovieDTO Movie { get; set; } = null!;

    public PagedResult<ReviewDTO> Reviews { get; set; } = new PagedResult<ReviewDTO>();
}
