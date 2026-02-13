// Global using directives

global using ApiService.DataAccess;

global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Logging;

global using MongoDB.Bson;
global using MongoDB.Driver;

global using Shared.Interfaces.Repository;
global using Shared.Interfaces.Services;
global using Shared.Models;
global using Shared.Models.DTOs;
// Type aliases for compatibility
global using MongoDbContextFactory = ApiService.DataAccess.IMongoDbContextFactory;
