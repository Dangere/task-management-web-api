using AutoMapper;
using TaskManagementWebAPI.Models.Entities;
using TaskManagementWebAPI.Models.DTOs.Tasks;
using TaskManagementWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementWebAPI.Services;
public class TaskServices(IMapper mapper, SyncoraDbContext dbContext)
{
    private readonly IMapper _mapper = mapper;
    private readonly SyncoraDbContext _dbContext = dbContext;


    public async Task<List<TaskDTO>> GetTaskDTOs()
    {
        // List<TaskDTO> taskDTOs = [];

        //this will load all entities into memory just to filter through them (bad approach)
        // await _dbContext.Tasks.ForEachAsync(t => taskDTOs.Add(_mapper.Map<TaskDTO>(t)));


        //this will run a `SELECT` sql query where it uses the TaskDTO properties as the selected columns
        return await _dbContext.Tasks.Select(t => _mapper.Map<TaskDTO>(t)).ToListAsync();
    }

    public async Task<TaskDTO?> GetTaskDTO(int id)
    {
        TaskEntity? taskEntity = await GetTaskEntity(id);
        if (taskEntity == null)
            return null;

        return _mapper.Map<TaskDTO>(taskEntity);
    }

    public async Task<TaskEntity?> GetTaskEntity(int id)
    {

        return await _dbContext.Tasks.FindAsync(id);
    }

    public async Task<TaskDTO> CreateTask(CreateTaskDTO newTaskDTO)
    {

        TaskEntity createdTask = new() { Title = newTaskDTO.Title, CreatedDate = DateTime.Now, OwnerUserId = newTaskDTO.OwnerId };

        await _dbContext.Tasks.AddAsync(createdTask);
        await _dbContext.SaveChangesAsync();

        return _mapper.Map<TaskDTO>(createdTask);
    }
    public async Task<bool> UpdateTaskAsync(int id, UpdateTaskDTO updatedTaskDTO)
    {

        TaskEntity? task = await GetTaskEntity(id);

        if (task == null)
            return false;

        task.Title = updatedTaskDTO.NewTitle ?? task.Title;
        task.Description = updatedTaskDTO.NewDescription ?? task.Description;
        task.Completed = updatedTaskDTO.Completed ?? task.Completed;

        if (updatedTaskDTO.Completed != null || updatedTaskDTO.NewTitle != null || updatedTaskDTO.NewDescription != null)
            task.UpdatedDate = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveTask(int id)
    {
        TaskEntity? task = await GetTaskEntity(id);

        if (task == null)
            return false;


        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync();

        return true;

    }
}