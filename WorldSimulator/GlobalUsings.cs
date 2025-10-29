global using System.Text.Json;
global using System.Threading.Channels;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Serilog;
global using WorldSimulator.Domain.ValueObjects;
global using WorldSimulator.Domain.Aggregates;
global using WorldSimulator.Domain.Services;
global using WorldSimulator.Domain.Events;
global using WorldSimulator.Domain.WorkPayloads;
global using WorldSimulator.Infrastructure.Persistence;
global using WorldSimulator.Infrastructure.Services;
global using WorldSimulator.Application.Factories;
// Commonly used value object types (import all for convenience)
global using GovernmentId = WorldSimulator.Domain.ValueObjects.GovernmentId;
global using SettlementId = WorldSimulator.Domain.ValueObjects.SettlementId;
global using PersonaId = WorldSimulator.Domain.ValueObjects.PersonaId;

