using DotNetCore.CAP;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMPL.Controllers
{
    [Route("movie")]
    public class MovieController : ControllerBase
    {
        private readonly IMovieQuery _movieQuery;
        private readonly IMediator _mediator;

        public MovieController(IMovieQuery movieQuery, IMediator mediator)
        {
            _movieQuery = movieQuery;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Search(MovieFiltering filtering, Sorting sorting, Paging paging)
        {
            var result = await _movieQuery.SearchAsync(filtering, sorting, paging);
            Response.Headers.Add("X-Pagination", result.PagingHeader.ConvertToJson());
            return StatusCode(StatusCodes.Status200OK, new ActionResult { Status = true, Data = new { result.PagingHeader, result.PagingData } });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody]MovieCreateInputModel input)
        {
            var result = await _mediator.Send(new MovieCreateRequest { InputModel = input });
            if (result.Status == false)
            {
                if (result.ResponseType == ResponseType.Duplicate)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new ActionResult { Status = false, Messages = new List<string> { "Create Duplicate" } });
                }
            }
            return StatusCode(StatusCodes.Status200OK, new ActionResult { Status = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody]MovieUpdateInputModel input)
        {
            if (input == null || id != input.Id)
                return StatusCode(StatusCodes.Status400BadRequest, new ActionResult { Status = false, Messages = new List<string> { "Update BadRequest" } });
            var result = await _mediator.Send(new MovieUpdateRequest { InputModel = input });
            if (result.Status == false)
            {
                if (result.ResponseType == ResponseType.Duplicate)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new ActionResult { Status = false, Messages = new List<string> { "Update Duplicate" } });
                }
            }
            return StatusCode(StatusCodes.Status200OK, new ActionResult { Status = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new MovieDeleteRequest { Id = id });
            if (result.Status == false)
            {
                if (result.ResponseType == ResponseType.NotFound)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new ActionResult { Status = false, Messages = new List<string> { "Delete NotFound" } });
                }
            }
            return StatusCode(StatusCodes.Status200OK, new ActionResult { Status = true });
        }

        [HttpPost(nameof(Import))]
        public IActionResult Import(IFormFile file)
        {
            return Ok(file.FileName);
        }

        [HttpPost(nameof(Export))]
        public IActionResult Export()
        {
            return Ok();
        }

        #region Subscriber
        [CapSubscribe("IMPL.MOVIE.UPDATE")]
        private void MovieUpdateEventHandler(MovieUpdateEvent ev)
        {
            var id = ev.Id;
            var name = ev.Name;
            var directorId = ev.DirectorId;
        }
        #endregion
    }
}
