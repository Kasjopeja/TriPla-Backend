using Microsoft.Extensions.DependencyInjection;
using TriPla.Backend.Application.Attractions;
using TriPla.Backend.Application.Auth;
using TriPla.Backend.Application.Comments;
using TriPla.Backend.Application.Expenses;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Application.Participants;
using TriPla.Backend.Application.Trips;

namespace TriPla.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<ITripHistoryService, TripHistoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IAttractionService, AttractionService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IParticipantService, ParticipantService>();

        return services;
    }
}
