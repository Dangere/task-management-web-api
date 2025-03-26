using AutoMapper;
using TaskManagementWebAPI.Models.Entities;
using TaskManagementWebAPI.Models.DTOs.Tasks;
using TaskManagementWebAPI.Data;
using Microsoft.EntityFrameworkCore;
using TaskManagementWebAPI.Utilities;

namespace TaskManagementWebAPI.Services;
public class TaskService(IMapper mapper, SyncoraDbContext dbContext)
{
    private readonly IMapper _mapper = mapper;
    private readonly SyncoraDbContext _dbContext = dbContext;

    public async Task<Result<List<TaskDTO>>> GetTasksForUser(int userId, int groupId)
    {
        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().Include(g => g.Tasks).SingleOrDefaultAsync(g => g.Id == groupId && g.OwnerUserId == userId);
        if (groupEntity == null)
            return Result<List<TaskDTO>>.Error("Group does not exist.", 404);

        List<TaskDTO> tasks = groupEntity.Tasks.OrderBy(t => t.CreationDate).Select(t => _mapper.Map<TaskDTO>(t)).ToList();

        return Result<List<TaskDTO>>.Success(tasks);
    }

    public async Task<Result<TaskDTO>> GetTaskForUser(int taskId, int userId, int groupId)
    {

        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
        if (groupEntity == null)
            return Result<TaskDTO>.Error("Group does not exist.", 404);

        TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(taskId);
        if (taskEntity == null)
            return Result<TaskDTO>.Error("Task does not exist.", 404);

        bool hasAccess = groupEntity.OwnerUserId == userId || groupEntity.Members.Any(u => u.Id == userId);

        if (!hasAccess)
            return Result<TaskDTO>.Error("User has no access to this task.", 403);
        return Result<TaskDTO>.Success(_mapper.Map<TaskDTO>(taskEntity));
    }

    public async Task<Result<string>> UpdateTaskForUser(int taskId, int groupId, int userId, UpdateTaskDTO updatedTaskDTO)
    {

        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId && g.OwnerUserId == userId);
        if (groupEntity == null)
            return Result<string>.Error("Group does not exist.", 404);


        TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(taskId);

        if (taskEntity == null)
            return Result<string>.Error("Task does not exist.", 404);

        bool isOwner = groupEntity.OwnerUserId == userId;
        bool isShared = groupEntity.Members.Any(u => u.Id == userId);
        if (!isOwner && isShared)
        {
            return Result<string>.Error("A shared user can't update the details of tasks in groups they don't own", 403);
        }
        else if (!isOwner && !isShared)
            return Result<string>.Error("User has no access to this task.", 403);

        return await UpdateTaskEntity(taskEntity, updatedTaskDTO);
    }

    public async Task<Result<string>> DeleteTaskForUser(int taskId, int groupId, int userId)
    {
        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId && g.OwnerUserId == userId);
        if (groupEntity == null)
            return Result<string>.Error("Group does not exist.", 404);


        TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(taskId);

        if (taskEntity == null)
            return Result<string>.Error("Task does not exist.", 404);

        bool isOwner = groupEntity.OwnerUserId == userId;
        bool isShared = groupEntity.Members.Any(u => u.Id == userId);
        if (!isOwner && isShared)
        {
            return Result<string>.Error("A shared user can't delete tasks in groups they don't own", 403);
        }
        else if (!isOwner && !isShared)
            return Result<string>.Error("User has no access to this task.", 403);

        _dbContext.Tasks.Remove(taskEntity);
        await _dbContext.SaveChangesAsync();

        return Result<string>.Success("Task deleted.");
    }


    // public async Task<Result<string>> AllowAccessToTask(int taskId, int userId, string userNameToGrant, bool allowAccess)
    // {
    //     TaskEntity? taskEntity = await _dbContext.Tasks.Include(t => t.SharedUsers).FirstOrDefaultAsync(t => t.Id == taskId);

    //     if (taskEntity == null)
    //         return Result<string>.Error("Task does not exist.", 404);

    //     UserEntity? userToGrant = await _dbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.UserName, userNameToGrant));

    //     if (userToGrant == null)
    //         return Result<string>.Error("User does not exist.", 404);


    //     if (allowAccess == taskEntity.SharedUsers.Any(u => u.Id == userToGrant.Id))
    //         return Result<string>.Error($"The user has already been " + (allowAccess ? "granted" : "revoked") + " access.", 403);


    //     bool isOwner = taskEntity.OwnerUserId == userId;
    //     bool isShared = taskEntity.SharedUsers.Any(u => u.Id == userId);
    //     if (!isOwner && isShared)
    //     {
    //         return Result<string>.Error("A shared user can't " + (allowAccess ? "grant" : "revoke") + " access to a task they don't own", 403);
    //     }
    //     else if (!isOwner && !isShared)
    //         return Result<string>.Error("User has no access to this task.", 403);

    //     if (allowAccess)
    //         taskEntity.SharedUsers.Add(userToGrant);
    //     else
    //         taskEntity.SharedUsers.Remove(userToGrant);

    //     await _dbContext.SaveChangesAsync();

    //     return Result<string>.Success(allowAccess ? "Access granted." : "Access revoked.");

    // }
    // public async Task<Result<List<TaskDTO>>> GetAllTaskDTOs()
    // {
    //     //this will load all entities into memory just to filter through them (bad approach)
    //     // await _dbContext.Tasks.ForEachAsync(t => taskDTOs.Add(_mapper.Map<TaskDTO>(t)));


    //     //this will run a `SELECT` sql query where it uses the TaskDTO properties as the selected columns

    //     List<TaskDTO> tasks = await _dbContext.Tasks.Include(t => t.SharedUsers).AsNoTracking().OrderBy(t => t.CreationDate).Select(t => _mapper.Map<TaskDTO>(t)).ToListAsync();

    //     return Result<List<TaskDTO>>.Success(tasks);
    // }

    // public async Task<Result<TaskDTO>> GetTaskDTO(int id)
    // {
    //     TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(id);
    //     if (taskEntity == null)
    //         return Result<TaskDTO>.Error("Task does not exist.", 404);

    //     return Result<TaskDTO>.Success(_mapper.Map<TaskDTO>(taskEntity));
    // }

    public async Task<Result<TaskDTO>> CreateTaskForUser(CreateTaskDTO newTaskDTO, int userId, int groupId)
    {
        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId && g.OwnerUserId == userId);
        if (groupEntity == null)
            return Result<TaskDTO>.Error("Group does not exist.", 404);

        TaskEntity createdTask = new() { Title = newTaskDTO.Title, Description = newTaskDTO.Description, CreationDate = DateTime.UtcNow, GroupId = groupId };

        await _dbContext.Tasks.AddAsync(createdTask);
        await _dbContext.SaveChangesAsync();

        return Result<TaskDTO>.Success(_mapper.Map<TaskDTO>(createdTask));
    }
    public async Task<Result<string>> UpdateTask(int id, int groupId, UpdateTaskDTO updatedTaskDTO)
    {
        GroupEntity? groupEntity = await _dbContext.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId);
        if (groupEntity == null)
            return Result<string>.Error("Group does not exist.", 404);

        TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(id);

        if (taskEntity == null)
            return Result<string>.Error("Task does not exist.", 404);

        return await UpdateTaskEntity(taskEntity, updatedTaskDTO);
    }

    public async Task<Result<string>> RemoveTask(int id)
    {
        TaskEntity? taskEntity = await _dbContext.Tasks.FindAsync(id);

        if (taskEntity == null)
            return Result<string>.Error("Task does not exist.", 404);



        _dbContext.Tasks.Remove(taskEntity);
        await _dbContext.SaveChangesAsync();

        return Result<string>.Success("Task deleted.");
    }

    private async Task<Result<string>> UpdateTaskEntity(TaskEntity taskEntity, UpdateTaskDTO updatedTaskDTO)
    {
        taskEntity.Title = updatedTaskDTO.Title ?? taskEntity.Title;
        taskEntity.Description = updatedTaskDTO.Description ?? taskEntity.Description;

        if (updatedTaskDTO.Title != null || updatedTaskDTO.Description != null)
            taskEntity.LastUpdateDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Result<string>.Success("Task updated.");
    }
}