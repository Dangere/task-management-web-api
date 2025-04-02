using System.Security.Claims;
using SyncoraBackend.Utilities;
using Microsoft.AspNetCore.Mvc;
using SyncoraBackend.Attributes;
using SyncoraBackend.Enums;
using SyncoraBackend.Models.DTOs.Tasks;
using SyncoraBackend.Services;


namespace SyncoraBackend.Controllers;

[AuthorizeRoles(UserRole.Admin, UserRole.User)]
[ApiController]
[Route("api/[controller]")]
public class UsersController(TaskService taskService) : ControllerBase
{
    private readonly TaskService _taskService = taskService;
    private const string _getTaskEndpointName = "GetTaskForUser";



}