using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TriPla.Backend.Api.Extensions;
using TriPla.Backend.Application.DTOs.Attractions;
using TriPla.Backend.Application.DTOs.Comments;
using TriPla.Backend.Application.DTOs.Expenses;
using TriPla.Backend.Application.DTOs.Participants;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Interfaces;

namespace TriPla.Backend.Api.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IAttractionService _attractionService;
    private readonly IExpenseService _expenseService;
    private readonly ICommentService _commentService;
    private readonly IParticipantService _participantService;
    private readonly ITripHistoryService _historyService;

    public TripsController(
        ITripService tripService,
        IAttractionService attractionService,
        IExpenseService expenseService,
        ICommentService commentService,
        IParticipantService participantService,
        ITripHistoryService historyService)
    {
        _tripService = tripService;
        _attractionService = attractionService;
        _expenseService = expenseService;
        _commentService = commentService;
        _participantService = participantService;
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _tripService.GetByUserAsync(userId, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _tripService.GetByIdAsync(id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTripRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _tripService.CreateAsync(userId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTripRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _tripService.UpdateAsync(id, userId, request, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _tripService.DeleteAsync(id, userId, ct);
        return result.ToActionResult();
    }

    // ---- Attractions ----

    [HttpGet("{id:guid}/attractions")]
    public async Task<IActionResult> GetAttractions(Guid id, CancellationToken ct) =>
        (await _attractionService.GetByTripAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/attractions")]
    public async Task<IActionResult> AddAttraction(Guid id, [FromBody] CreateAttractionRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _attractionService.AddToTripAsync(id, userId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetAttractions), new { id }, result.Value);
    }

    [HttpPut("attractions/{attractionId:guid}")]
    public async Task<IActionResult> UpdateAttraction(Guid attractionId, [FromBody] CreateAttractionRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _attractionService.UpdateAsync(attractionId, userId, request, ct)).ToActionResult();
    }

    [HttpDelete("attractions/{attractionId:guid}")]
    public async Task<IActionResult> DeleteAttraction(Guid attractionId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _attractionService.DeleteAsync(attractionId, userId, ct)).ToActionResult();
    }

    // ---- Expenses ----

    [HttpGet("{id:guid}/expenses")]
    public async Task<IActionResult> GetExpenses(Guid id, CancellationToken ct) =>
        (await _expenseService.GetByTripAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/expenses")]
    public async Task<IActionResult> AddExpense(Guid id, [FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _expenseService.AddToTripAsync(id, userId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetExpenses), new { id }, result.Value);
    }

    [HttpPut("expenses/{expenseId:guid}")]
    public async Task<IActionResult> UpdateExpense(Guid expenseId, [FromBody] UpdateExpenseRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _expenseService.UpdateAsync(expenseId, userId, request, ct)).ToActionResult();
    }

    [HttpPut("expenses/{expenseId:guid}/settled")]
    public async Task<IActionResult> SetExpenseSettled(Guid expenseId, [FromBody] SetSettledRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _expenseService.SetSettledAsync(expenseId, userId, request.IsSettled, ct)).ToActionResult();
    }

    [HttpPut("{id:guid}/expenses/settled-all")]
    public async Task<IActionResult> SetAllExpensesSettled(Guid id, [FromBody] SetSettledRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _expenseService.SetAllSettledAsync(id, userId, request.IsSettled, ct)).ToActionResult();
    }

    [HttpDelete("expenses/{expenseId:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid expenseId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _expenseService.DeleteAsync(expenseId, userId, ct)).ToActionResult();
    }

    // ---- Comments ----

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct) =>
        (await _commentService.GetByTripAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await _commentService.AddToTripAsync(id, userId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetComments), new { id }, result.Value);
    }

    [HttpPut("comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _commentService.UpdateAsync(commentId, userId, request, ct)).ToActionResult();
    }

    [HttpDelete("comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _commentService.DeleteAsync(commentId, userId, ct)).ToActionResult();
    }

    // ---- Participants ----

    [HttpGet("{id:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid id, CancellationToken ct) =>
        (await _participantService.GetByTripAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipant(Guid id, [FromBody] AddParticipantRequest request, CancellationToken ct)
    {
        var requesterId = this.GetUserId();
        var result = await _participantService.AddAsync(id, requesterId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetParticipants), new { id }, result.Value);
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid userId, CancellationToken ct)
    {
        var requesterId = this.GetUserId();
        return (await _participantService.RemoveAsync(id, requesterId, userId, ct)).ToActionResult();
    }

    [HttpPut("{id:guid}/participants/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        var requesterId = this.GetUserId();
        return (await _participantService.ChangeRoleAsync(id, requesterId, userId, request.Role, ct)).ToActionResult();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, [FromQuery] int limit = 100, CancellationToken ct = default) =>
        (await _historyService.GetAsync(id, limit, ct)).ToActionResult();

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        var userId = this.GetUserId();
        return (await _participantService.LeaveTripAsync(id, userId, ct)).ToActionResult();
    }
}
