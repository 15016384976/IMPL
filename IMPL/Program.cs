using DotNetCore.CAP;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace IMPL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }

    #region Entity
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DirectorId { get; set; }
    }

    public class Director
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MovieActor
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public int ActorId { get; set; }
    }
    #endregion

    #region Configuration
    public class MovieConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.ToTable(nameof(Movie));
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).HasMaxLength(50).IsRequired(true);
            builder.Property(v => v.DirectorId).IsRequired(true);
        }
    }

    public class DirectorConfiguration : IEntityTypeConfiguration<Director>
    {
        public void Configure(EntityTypeBuilder<Director> builder)
        {
            builder.ToTable(nameof(Director));
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).HasMaxLength(50).IsRequired(true);
        }
    }

    public class ActorConfiguration : IEntityTypeConfiguration<Actor>
    {
        public void Configure(EntityTypeBuilder<Actor> builder)
        {
            builder.ToTable(nameof(Actor));
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).HasMaxLength(50).IsRequired(true);
        }
    }

    public class MovieActorConfiguration : IEntityTypeConfiguration<MovieActor>
    {
        public void Configure(EntityTypeBuilder<MovieActor> builder)
        {
            builder.ToTable(nameof(MovieActor));
            builder.HasKey(v => v.Id);
            builder.Property(v => v.MovieId).IsRequired(true);
            builder.Property(v => v.ActorId).IsRequired(true);
        }
    }
    #endregion

    #region Database
    public class Database : DbContext
    {
        public Database(DbContextOptions<Database> dbContextOptions) : base(dbContextOptions)
        {

        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Director> Directors { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new MovieConfiguration());
            modelBuilder.ApplyConfiguration(new DirectorConfiguration());
            modelBuilder.ApplyConfiguration(new ActorConfiguration());
            modelBuilder.ApplyConfiguration(new MovieActorConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
    #endregion

    #region Enum
    public enum ResponseType
    {
        NotFound,
        Duplicate
    } 
    #endregion

    #region Result
    public class ActionResult
    {
        public bool Status { get; set; } = true;
        public List<string> Messages { get; set; }
        public object Data { get; set; }
    }

    public class RequestResult
    {
        public bool Status { get; set; } = true;
        public ResponseType ResponseType { get; set; }
    }
    #endregion

    #region ObjectResult
    public class InternalServerErrorObjectResult : ObjectResult
    {
        public InternalServerErrorObjectResult(object value) : base(value)
        {
            StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
    #endregion

    #region Filter
    public class ActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var actionResult = new ActionResult { Status = false, Messages = new List<string>() };
                foreach (var value in context.ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        actionResult.Messages.Add(error.ErrorMessage);
                    }
                }
                context.Result = new BadRequestObjectResult(actionResult);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }

    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var actionResult = new ActionResult { Status = false, Messages = new List<string>() { context.Exception.Message } };
            context.Result = new InternalServerErrorObjectResult(actionResult);
        }
    }
    #endregion

    #region Query
    public interface IMovieQuery
    {
        Task<PagingResult<MovieSearchOutputModel>> SearchAsync(MovieFiltering filtering, Sorting sorting, Paging paging);
    }

    public class MovieQuery : IMovieQuery
    {
        private readonly IMovieRepository _movieRepository;

        public MovieQuery(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<PagingResult<MovieSearchOutputModel>> SearchAsync(MovieFiltering filtering, Sorting sorting, Paging paging)
        {
            return await _movieRepository.SearchAsync(filtering, sorting, paging);
        }
    }
    #endregion

    #region Request
    public class MovieCreateRequest : IRequest<RequestResult>
    {
        public MovieCreateInputModel InputModel { get; set; }
    }

    public class MovieCreateRequestHandler : IRequestHandler<MovieCreateRequest, RequestResult>
    {
        private readonly IMovieRepository _movieRepository;

        public MovieCreateRequestHandler(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<RequestResult> Handle(MovieCreateRequest request, CancellationToken cancellationToken)
        {
            return await _movieRepository.CreateAsync(request.InputModel);
        }
    }

    public class MovieUpdateRequest : IRequest<RequestResult>
    {
        public MovieUpdateInputModel InputModel { get; set; }
    }

    public class MovieUpdateRequestHandler : IRequestHandler<MovieUpdateRequest, RequestResult>
    {
        private readonly IMovieRepository _movieRepository;

        public MovieUpdateRequestHandler(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<RequestResult> Handle(MovieUpdateRequest request, CancellationToken cancellationToken)
        {
            return await _movieRepository.UpdateAsync(request.InputModel);
        }
    }

    public class MovieDeleteRequest : IRequest<RequestResult>
    {
        public int Id { get; set; }
    }

    public class MovieDeleteRequestHandler : IRequestHandler<MovieDeleteRequest, RequestResult>
    {
        private readonly IMovieRepository _movieRepository;

        public MovieDeleteRequestHandler(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<RequestResult> Handle(MovieDeleteRequest request, CancellationToken cancellationToken)
        {
            return await _movieRepository.DeleteAsync(request.Id);
        }
    }
    #endregion

    #region InputModel
    public class MovieCreateInputModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public int DirectorId { get; set; }
    }

    public class MovieUpdateInputModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int DirectorId { get; set; }
    }
    #endregion

    #region OutputModel
    public class MovieSearchOutputModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Director { get; set; }
        public IQueryable<string> Actors { get; set; }
    }

    public class MovieOutputModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DirectorOutputModel Director { get; set; }
        public List<ActorOutputModel> Actors { get; set; }
    }

    public class DirectorOutputModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ActorOutputModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    #endregion

    #region Repository
    public interface IMovieRepository
    {
        Task<PagingResult<MovieSearchOutputModel>> SearchAsync(MovieFiltering filtering, Sorting sorting, Paging paging);
        Task<RequestResult> CreateAsync(MovieCreateInputModel input);
        Task<RequestResult> UpdateAsync(MovieUpdateInputModel input);
        Task<RequestResult> DeleteAsync(int id);
    }

    public class MovieRepository : IMovieRepository
    {
        private readonly Database _database;
        private readonly ICapPublisher _capPublisher;

        public MovieRepository(Database database, ICapPublisher capPublisher)
        {
            _database = database;
            _capPublisher = capPublisher;
        }

        public async Task<PagingResult<MovieSearchOutputModel>> SearchAsync(MovieFiltering filtering, Sorting sorting, Paging paging)
        {
            var queryable = from movie in _database.Movies
                            join director in _database.Directors on movie.DirectorId equals director.Id
                            into tmp
                            from t in tmp.DefaultIfEmpty() // t = IEnumerable<Director>
                            select new MovieSearchOutputModel
                            {
                                Id = movie.Id,
                                Name = movie.Name,
                                Director = t == null ? "" : t.Name,
                                Actors = from actor in _database.Actors
                                         join movieActor in _database.MovieActors on actor.Id equals movieActor.ActorId
                                         where movieActor.MovieId == movie.Id
                                         select actor.Name
                            };

            #region filtering
            var name = filtering.Name?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(name))
            {
                queryable = queryable.Where(v => v.Name.ToLowerInvariant().Contains(name));
            }
            #endregion

            #region sorting
            if (!string.IsNullOrEmpty(sorting.SortBy))
            {
                queryable = queryable.SortBy(sorting.SortBy);
            }
            #endregion

            #region paging
            var pageNumber = paging.PageNumber;
            var pageSize = paging.PageSize;
            var totalCount = await queryable.CountAsync();
            var totalPage = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasPrevPage = pageNumber > 1;
            var hasNextPage = pageNumber < totalPage;
            var prevPageNumber = hasPrevPage ? pageNumber - 1 : 1;
            var nextPageNumber = hasNextPage ? pageNumber + 1 : totalPage;
            #endregion

            return new PagingResult<MovieSearchOutputModel>
            {
                PagingHeader = new PagingHeader(pageNumber, pageSize, totalCount, totalPage, hasPrevPage, hasNextPage, prevPageNumber, nextPageNumber),
                PagingData = await queryable.Skip(paging.PageSize * (paging.PageNumber - 1)).Take(paging.PageSize).ToListAsync()
            };
        }

        public async Task<RequestResult> CreateAsync(MovieCreateInputModel input)
        {
            var entity = await _database.Movies.Where(v => v.Name == input.Name).FirstOrDefaultAsync();
            if (entity == null)
            {
                using (var transaction = _database.Database.BeginTransaction(_capPublisher, true))
                {
                    try
                    {
                        var movie = new Movie { Name = input.Name, DirectorId = input.DirectorId };
                        _database.Movies.Add(movie);
                        await _database.SaveChangesAsync();
                        /*
                        foreach (var actor in input.Actors)
                            await _database.MovieActors.AddAsync(new MovieActor { MovieId = movie.Id, ActorId = actor.ActorId });
                        await _database.SaveChangesAsync();
                        */
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex.InnerException;
                    }
                }
                return new RequestResult { Status = true };
            }
            else
            {
                return new RequestResult { Status = false, ResponseType = ResponseType.Duplicate };
            }
        }

        public async Task<RequestResult> UpdateAsync(MovieUpdateInputModel input)
        {
            var entity = await _database.Movies.Where(v => v.Name == input.Name && v.Id != input.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                using (var transaction = _database.Database.BeginTransaction(_capPublisher, true))
                {
                    try
                    {
                        var movie = new Movie { Id = input.Id, Name = input.Name, DirectorId = input.DirectorId };
                        _database.Movies.Update(movie);
                        await _database.SaveChangesAsync();
                        #region Publisher
                        _capPublisher.Publish("IMPL.MOVIE.UPDATE", new MovieUpdateEvent
                        {
                            Id = movie.Id,
                            Name = movie.Name,
                            DirectorId = movie.DirectorId
                        });
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex.InnerException;
                    }
                }
                return new RequestResult { Status = true };
            }
            else
            {
                return new RequestResult { Status = false, ResponseType = ResponseType.Duplicate };
            }
        }

        public async Task<RequestResult> DeleteAsync(int id)
        {
            var entity = await _database.Movies.Where(v => v.Id == id).FirstOrDefaultAsync();
            if (entity == null)
                return new RequestResult { Status = false, ResponseType = ResponseType.NotFound };
            using (var transaction = _database.Database.BeginTransaction(_capPublisher, true))
            {
                try
                {
                    _database.Movies.Remove(entity);
                    _database.MovieActors.RemoveRange(_database.MovieActors.Where(v => v.MovieId == id).ToList());
                    await _database.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex.InnerException;
                }
            }
            return new RequestResult { Status = true };
        }
    }
    #endregion

    #region Filtering
    public class MovieFiltering
    {
        public string Name { get; set; } = "";
    }
    #endregion

    #region Sorting
    public class Sorting
    {
        public string SortBy { get; set; } = "";
    }

    public static class SortingExtension
    {
        public static IQueryable<T> SortBy<T>(this IQueryable<T> queryable, string sortby)
        {
            var expression = string.Empty;
            foreach (var item in sortby.Split(','))
                expression += AdjustDirection(item) + ",";
            expression = expression.Substring(0, expression.Length - 1);
            try
            {
                queryable = queryable.OrderBy(expression);
            }
            catch (ParseException)
            {
                // include field not part of the model
            }
            return queryable;
        }

        private static string AdjustDirection(string item)
        {
            if (item.Contains(' ') == false)
                return item; // no direction specified
            var field = item.Split(' ')[0];
            var direction = item.Split(' ')[1];
            switch (direction)
            {
                case "asc":
                case "ascending":
                    return field + " ascending";
                case "desc":
                case "descending":
                    return field + " descending";
                default:
                    return field;
            };
        }
    }
    #endregion

    #region Paging
    public class Paging
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }

    public class PagingHeader
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPage { get; }
        public bool HasPrevPage { get; }
        public bool HasNextPage { get; }
        public int PrevPageNumber { get; }
        public int NextPageNumber { get; }

        public PagingHeader(int pageNumber, int pageSize, int totalCount, int totalPage, bool hasPrevPage, bool hasNextPage, int prevPageNumber, int nextPageNumber)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPage = totalPage;
            HasPrevPage = hasPrevPage;
            HasNextPage = hasNextPage;
            PrevPageNumber = prevPageNumber;
            NextPageNumber = nextPageNumber;
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }
    }

    public class PagingResult<T> where T : class
    {
        public PagingHeader PagingHeader { get; set; }
        public List<T> PagingData { get; set; }
    }
    #endregion

    #region Event
    public class MovieUpdateEvent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DirectorId { get; set; }
    }
    #endregion
}
