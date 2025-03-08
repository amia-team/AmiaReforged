// using AmiaReforged.Core.UserInterface;
// using AmiaReforged.PwEngine.Database;
// using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
// using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;
// using AmiaReforged.PwEngine.Systems.WindowingSystem;
// using Anvil.API;
// using Anvil.API.Events;
// using Anvil.Services;
// using NWN.Core;
//
// namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;
//
// public class LedgerView : NuiView<LedgerView>
// {
//     public override string Id => "pw_job_ledger";
//     public sealed override string Title => "Ledger";
//     public override NuiWindow? WindowTemplate { get; }
//
//     public override INuiController? CreateDefaultController(NwPlayer player)
//     {
//         return CreateController<LedgerController>(player);
//     }
//
//     public readonly NuiBind<string> LedgerEntries = new("ledger_entries");
//     public readonly NuiBind<string> StockpileCount = new("stockpile_count");
//     public readonly NuiBind<string> PlayerCount = new("player_count");
//     public readonly NuiBind<int> LedgerEntryCount = new("ledger_entry_count");
//
//     public NuiButton LogsButton;
//     public NuiButton PlanksButton;
//     public NuiButton OreButton;
//     public NuiButton IngotsButton;
//     public NuiButton GrainButton;
//     public NuiButton FlourButton;
//     public NuiButton IngredientsButton;
//     public NuiButton FoodButton;
//     public NuiButton DrinksButton;
//     public NuiButton AcademiaButton;
//     public NuiButton PeltsButton;
//     public NuiButton WeaponsButton;
//     public NuiButton ArmorButton;
//     public NuiButton JewelryButton;
//
//     public LedgerView()
//     {
//         List<NuiListTemplateCell> overviewCells = new()
//         {
//             new NuiListTemplateCell(new NuiLabel(LedgerEntries)
//             {
//                 VerticalAlign = NuiVAlign.Middle,
//             }),
//             new NuiListTemplateCell(new NuiLabel(StockpileCount)),
//             new NuiListTemplateCell(new NuiLabel(PlayerCount))
//         };
//
//         NuiRow root = new()
//         {
//             Children = new List<NuiElement>
//             {
//                 new NuiColumn
//                 {
//                     Children = new List<NuiElement>
//                     {
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Logs")
//                                 {
//                                     Id = "logs_overview",
//                                     Width = 100,
//                                 }.Assign(out LogsButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Planks")
//                                 {
//                                     Id = "planks_overview",
//                                     Width = 100,
//                                 }.Assign(out PlanksButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Ore")
//                                 {
//                                     Id = "ore_overview",
//                                     Width = 100,
//                                 }.Assign(out OreButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Ingots")
//                                 {
//                                     Id = "ingots_overview",
//                                     Width = 100,
//                                 }.Assign(out IngotsButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Grain")
//                                 {
//                                     Id = "grain_overview",
//                                     Width = 100,
//                                 }.Assign(out GrainButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Flour")
//                                 {
//                                     Id = "flour_overview",
//                                     Width = 100,
//                                 }.Assign(out FlourButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Ingredients")
//                                 {
//                                     Id = "ingredients_overview",
//                                     Width = 100,
//                                 }.Assign(out IngredientsButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Food")
//                                 {
//                                     Id = "food_overview",
//                                     Width = 100,
//                                 }.Assign(out FoodButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Drinks")
//                                 {
//                                     Id = "drinks_overview",
//                                     Width = 100,
//                                 }.Assign(out DrinksButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Academia")
//                                 {
//                                     Id = "academia_overview",
//                                     Width = 100,
//                                 }.Assign(out AcademiaButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Pelts")
//                                 {
//                                     Id = "pelts_overview",
//                                     Width = 100,
//                                 }.Assign(out PeltsButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Weapons")
//                                 {
//                                     Id = "weapons_overview",
//                                     Width = 100,
//                                 }.Assign(out WeaponsButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Armor")
//                                 {
//                                     Id = "armor_overview",
//                                     Width = 100,
//                                 }.Assign(out ArmorButton)
//                             }
//                         },
//                         new NuiRow
//                         {
//                             Children = new List<NuiElement>
//                             {
//                                 new NuiButton("Jewelry")
//                                 {
//                                     Id = "jewelry_overview",
//                                     Width = 100,
//                                 }.Assign(out JewelryButton)
//                             }
//                         }
//                     }
//                 },
//                 new NuiColumn()
//                 {
//                     Children = new List<NuiElement>()
//                     {
//                         new NuiRow()
//                         {
//                             Children = new List<NuiElement>()
//                             {
//                                 new NuiLabel("Item"),
//                                 new NuiLabel("Stockpile"),
//                                 new NuiLabel("Player"),
//                             }
//                         },
//                         new NuiList(overviewCells, LedgerEntryCount)
//                         {
//                             RowHeight = 35f,
//                         }
//                     }
//                 }
//             }
//         };
//
//         WindowTemplate = new NuiWindow(root, Title)
//         {
//             Geometry = new NuiRect(0f, 100f, 600f, 700f),
//         };
//     }
// }
//
// public class LedgerController : NuiController<LedgerView>
// {
//     private Ledger _storageLedger = null!;
//     private Ledger _playerLedger = null!;
//     private ItemType _currentType;
//
//     [Inject] private Lazy<PwEngineContext?> Context { get; set; }
//     [Inject] private Lazy<LedgerLoader?> LedgerLoader { get; set; }
//
//     public override void Init()
//     {
//         _currentType = ItemType.Log;
//         Token.Player.SendServerMessage("yeet.");
//         // long? storageId = long.Parse(NWScript.GetLocalString(Token.Player.LoginCreature, "accessed_storage_id"));
//         long? storageId = 0;
//
//         ItemStorage storage = new ItemStorage()
//         {
//             Items = new List<StoredJobItem>()
//             {
//             }
//         };
//         // ItemStorage? storage = Context.Value?.StorageContainers.SingleOrDefault(s => s.Id == storageId);
//         // if (storage == null)
//         // {
//         //     Token.Player.SendServerMessage("Could not find the storage container you were looking for.");
//         //
//         //     Token.Close();
//         //
//         //     return;
//         // }
//
//         _storageLedger = LedgerLoader.Value?.FromItemStorage(storage)!;
//         _playerLedger = LedgerLoader.Value?.FromPlayer(Token.Player.LoginCreature!)!;
//
//         Token.Player.SendServerMessage(_playerLedger.ToString());
//
//         RefreshLedgerEntries();
//     }
//
//     private void RefreshLedgerEntries()
//     {
//         // Given we're trying to display all items between both the stockpile and the player, we're
//         // Utilizing our unified list of item names to ensure that every item of the current ItemType is displayed...
//
//         Token.Player.SendServerMessage($"You have {_playerLedger.Entries.Count} entries in your ledger.");
//
//         List<LedgerEntry> selectedEntries = _storageLedger.Entries.Where(e => e.Type == _currentType).ToList();
//
//         selectedEntries.AddRange(_playerLedger.Entries.Where(e => e.Type == _currentType));
//         
//         Token.Player.SendServerMessage($"The storage stockpile has like {_storageLedger.Entries.Count} entries.");
//
//         selectedEntries = selectedEntries.DistinctBy(i => i.Name).ToList();
//
//
//         List<string> storageQuantities = new();
//         List<string> playerQuantities = new();
//         foreach (LedgerEntry t in selectedEntries)
//         {
//             storageQuantities.Add(_storageLedger.Entries.FirstOrDefault(e => e.Name == t.Name)?.Quantity.ToString() ?? "0");
//             playerQuantities.Add(_playerLedger.Entries.FirstOrDefault(e => e.Name == t.Name)?.Quantity.ToString() ?? "0");
//         }
//
//         Token.SetBindValues(View.LedgerEntries, selectedEntries.Select(e => e.Name).ToList());
//         Token.SetBindValues(View.StockpileCount, storageQuantities);
//         Token.SetBindValues(View.PlayerCount, playerQuantities);
//         
//         Token.SetBindValue(View.LedgerEntryCount, selectedEntries.Count);
//         Token.Player.SendServerMessage($"Refreshed ledger entries. {selectedEntries.Count} entries available.");
//     }
//
//     public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
//     {
//     }
//
//     protected override void OnClose()
//     {
//     }
// }

