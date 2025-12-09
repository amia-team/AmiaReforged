namespace AmiaReforged.PwEngine.Features.Module.DeveloperTools;

/// <summary>
/// Contains name dictionaries for random citizen generation.
/// </summary>
public static class CitizenNameData
{
    // Human Names
    public static readonly string[] HumanMaleFirstNames =
    [
        "Alden", "Arlen", "Arthur", "Barret", "Bastian", "Bram", "Bramwell", "Branick", "Byron", "Cade",
        "Cassius", "Cedran", "Cedric", "Conrad", "Corbin", "Corwin", "Cyrus", "Damon", "Darian", "Davin",
        "Derek", "Dorian", "Duncan", "Eli", "Ellis", "Elric", "Emeric", "Emmett", "Errich", "Evander",
        "Fenrick", "Finnan", "Fintan", "Fletcher", "Florian", "Garret", "Garran", "Gideon", "Godfrey", "Hadrian",
        "Halden", "Isidore", "Ivor", "Jareth", "Jasper", "Jossan", "Kael", "Keane", "Kendrick", "Landon",
        "Leif", "Lindal", "Loric", "Lucan", "Lucius", "Lyle", "Maddox", "Maric", "Merrick", "Milo",
        "Neville", "Nolan", "Nyle", "Odin", "Oren", "Orion", "Orson", "Osric", "Oswin", "Pax",
        "Percival", "Perrin", "Prentice", "Quentin", "Reed", "Roderick", "Roarke", "Ronan", "Rowan", "Silas",
        "Stellan", "Talbot", "Tavian", "Thane", "Theron", "Tiber", "Torren", "Vance", "Varian", "Victor",
        "Vincent", "Warrick", "Wellby", "Wesley", "Winston", "Xander", "Xavier", "Yancy", "Yorick", "Zane"
    ];

    public static readonly string[] HumanFemaleFirstNames =
    [
        "Adele", "Adria", "Aelene", "Alaina", "Alessa", "Alinor", "Amara", "Arden", "Arilla", "Ariselle",
        "Asha", "Astra", "Aveline", "Brenna", "Briala", "Brynn", "Caela", "Calia", "Callis", "Carina",
        "Celia", "Corinne", "Daelia", "Dalia", "Danica", "Darianne", "Delane", "Delia", "Dessa", "Eira",
        "Elara", "Elin", "Elira", "Elise", "Elizabeth", "Ellyn", "Elora", "Embera", "Emeline", "Emira",
        "Esme", "Evaline", "Evana", "Evelyn", "Fara", "Fenna", "Fiora", "Fiona", "Galenne", "Gweneth",
        "Gwynna", "Halene", "Helia", "Idara", "Ilena", "Imara", "Inara", "Isolde", "Jalene", "Jessa",
        "Jilian", "Jora", "Kaelia", "Kallia", "Kara", "Katria", "Keira", "Kelyn", "Kerise", "Laina",
        "Lara", "Leora", "Liriel", "Lirana", "Lorena", "Lyria", "Maela", "Marin", "Marisse", "Mira",
        "Morwen", "Myra", "Nalene", "Neria", "Nimera", "Nissa", "Olira", "Olivia", "Orlena", "Raina",
        "Relia", "Renna", "Sadia", "Selene", "Seris", "Talia", "Thalia", "Vessa", "Ysolde", "Zara"
    ];

    public static readonly string[] HumanLastNames =
    [
        "Ashwinter", "Barrowdell", "Blackthorne", "Brightwater", "Briarhollow", "Caskbow", "Claymere", "Daerlin", "Dalton", "Dawnmere",
        "Deepwell", "Durnford", "Eastmark", "Eldenhart", "Fairholt", "Farnath", "Fletworth", "Goldmantle", "Greythorn", "Halveric",
        "Hartwell", "Highbrook", "Ironbriar", "Ivydell", "Kingsford", "Lanton", "Longmere", "Maerholt", "Norcrest", "Oakmantle",
        "Pendrel", "Quillstone", "Redwyne", "Rivereth", "Rowanvale", "Seldrake", "Silvershade", "Smith", "Southmere", "Stonebrook",
        "Tarmond", "Tesselwood", "Thornwall", "Tressandar", "Valehart", "Varrow", "Waymire", "Whitford", "Windhaven", "Wylmar"
    ];

    // Elf Names
    public static readonly string[] ElfMaleFirstNames =
    [
        "Aelar", "Aelion", "Aerendyl", "Aerinor", "Aerris", "Ailmon", "Aithlin", "Alalthor", "Alion", "Althas",
        "Amrion", "Anarith", "Aramil", "Arannis", "Arathor", "Aravorn", "Ardolin", "Arelon", "Arthion", "Athlan",
        "Baelar", "Baerion", "Beluar", "Caelisar", "Caelthas", "Calion", "Calithor", "Callisarr", "Camlon", "Corathil",
        "Daelion", "Daereth", "Daevin", "Dalanther", "Damariel", "Dathlan", "Doreal", "Eiandel", "Elandor", "Elaith",
        "Elanthir", "Elathar", "Eldarion", "Eldrin", "Elion", "Ellisar", "Eremien", "Erunion", "Faelar", "Faeranduil",
        "Falandor", "Farlion", "Firaeth", "Gaelthorn", "Galanodel", "Halien", "Hathion", "Iarlos", "Ilian", "Illithor",
        "Ilnaris", "Inthil", "Isanthar", "Kelior", "Korathil", "Laeroth", "Larion", "Lathanderil", "Lithan", "Lorion",
        "Maeral", "Maereth", "Maethor", "Malion", "Mellian", "Merethir", "Naerion", "Nalthor", "Nalvaen", "Narin",
        "Nathalon", "Neldor", "Olorin", "Orlathas", "Pelior", "Quarion", "Rathlar", "Relion", "Selnaris", "Selthor",
        "Sorion", "Talarion", "Theralin", "Therris", "Thilior", "Urian", "Valanthir", "Varion", "Zalithar", "Zennorin"
    ];

    public static readonly string[] ElfFemaleFirstNames =
    [
        "Aelira", "Aenari", "Aeralyn", "Aeriselle", "Ailune", "Ainatha", "Aleris", "Althaea", "Amariel", "Amethra",
        "Anariel", "Anesha", "Aralyth", "Arelune", "Ariniel", "Arinya", "Ariselle", "Ariwyn", "Athlian", "Baelira",
        "Caelith", "Caelynn", "Calithra", "Calune", "Camea", "Ciriathe", "Daelira", "Daenyth", "Dahlune", "Delanora",
        "Delathrae", "Desmira", "Eilathra", "Eiluned", "Elanil", "Elaria", "Elathiel", "Elenna", "Elesse", "Elira",
        "Ellanie", "Ellaris", "Elowyn", "Elyandra", "Emelisse", "Enaris", "Enyale", "Eristra", "Faelune", "Faleris",
        "Feylana", "Feylira", "Galisse", "Gaeriel", "Haelira", "Halathrae", "Helysse", "Ilara", "Illyndra", "Ilyssae",
        "Inalyn", "Irieth", "Isylle", "Kaelira", "Kaelysse", "Kallira", "Laira", "Laeriel", "Lathienne", "Lethira",
        "Lilathrae", "Lirae", "Lirien", "Lysandra", "Maelith", "Maenira", "Maerielle", "Malira", "Meliantha", "Mellira",
        "Naelune", "Naeryss", "Nallira", "Nerisse", "Nethra", "Olariel", "Orlithra", "Pelune", "Quessara", "Raelisse",
        "Saniarae", "Selanna", "Seralyth", "Sylana", "Talira", "Thalyn", "Valisse", "Veranna", "Ylathra", "Zaleris"
    ];

    public static readonly string[] ElfLastNames =
    [
        "Amastacia", "Aelorath", "Brightleaf", "Cinderglade", "Duskwhisper", "Elarion", "Elmwood", "Eveningfall", "Faerondyl", "Farlaithe",
        "Fernshade", "Firaelion", "Flamepetal", "Fogwhisper", "Galethorn", "Glimmerleaf", "Goldenharp", "Highwind", "Ilitharion", "Kelstaris",
        "Laerithil", "Leafbow", "Leafwhisper", "Lightrain", "Moonbrook", "Moondell", "Moonwhisper", "Narthalen", "Nightbloom", "Nightglade",
        "Riverlune", "Shadowbriar", "Shadowglen", "Silverwind", "Silverpetal", "Softwind", "Starbrook", "Starweaver", "Sunwhisper", "Swiftbloom",
        "Thornstar", "Trueleaf", "Velarion", "Whisperleaf", "Whisperwind", "Whitepetal", "Windbloom", "Windrider", "Winterleaf", "Yavandar"
    ];

    // Dwarf Names
    public static readonly string[] DwarfMaleFirstNames =
    [
        "Abarik", "Adrik", "Aldram", "Alvinor", "Ardak", "Arngrim", "Baerdin", "Baern", "Balgrom", "Bardin",
        "Bargrum", "Beldrak", "Beldrum", "Belmarr", "Bergar", "Berrik", "Bhaldor", "Bhalrim", "Bharrom", "Bramdur",
        "Bralin", "Branik", "Brannik", "Brolthor", "Brottin", "Bryndur", "Dalgrom", "Dalmarr", "Darrak", "Deldrim",
        "Delgorn", "Dornic", "Draelor", "Drakkur", "Dravin", "Dromgar", "Durmarr", "Durthan", "Dwalim", "Dwarkin",
        "Eldram", "Eldrin", "Fargrim", "Farrak", "Feldrak", "Felnar", "Fendrin", "Forgrin", "Galdrum", "Galrik",
        "Garrond", "Geldram", "Gendril", "Gimrak", "Gorim", "Gorrik", "Granir", "Grathor", "Grendin", "Grimmar",
        "Grondar", "Gromm", "Haldor", "Halgrin", "Halkin", "Hargrum", "Hildrak", "Hurnic", "Jhadrin", "Jorgrin",
        "Kaldric", "Karnak", "Karron", "Keldrim", "Kendrak", "Khondram", "Kilvar", "Korgrim", "Kordran", "Kragur",
        "Kurdan", "Magrim", "Morgran", "Naldor", "Nargrom", "Norik", "Odrin", "Olgrom", "Ordrak", "Ragdin",
        "Raldorn", "Randram", "Rathrik", "Rhorik", "Thalgrin", "Thordram", "Thorin", "Tormarr", "Ulgar", "Vandrin"
    ];

    public static readonly string[] DwarfFemaleFirstNames =
    [
        "Adrana", "Aldra", "Almara", "Arditha", "Arduna", "Balgra", "Beldra", "Belmara", "Berris", "Bhalra",
        "Bhrilda", "Brana", "Branna", "Bralda", "Bralyn", "Brenna", "Brylla", "Dagna", "Dahlra", "Dalmira",
        "Dandria", "Deldra", "Deldrisa", "Dhorla", "Dhrissa", "Dorana", "Dornella", "Dramli", "Dranella", "Drassa",
        "Drelda", "Drelmara", "Drilda", "Durra", "Dwalla", "Eldra", "Eldrida", "Elmora", "Farli", "Fendra",
        "Fendris", "Fialra", "Filda", "Fralyn", "Frenna", "Galda", "Galmira", "Gathra", "Gelda", "Gelra",
        "Gerda", "Gilda", "Gimra", "Gindra", "Gisla", "Gralda", "Grania", "Grenda", "Grilda", "Grissa",
        "Gruna", "Gurda", "Haldris", "Haldra", "Halmira", "Harma", "Helda", "Hendris", "Hildra", "Hlinna",
        "Ilgra", "Ilmara", "Jarlda", "Jasmira", "Jilda", "Kaldra", "Kalmira", "Kandris", "Kendra", "Khilda",
        "Kilna", "Korla", "Kundra", "Maldra", "Maraeth", "Marlene", "Mavra", "Merda", "Midra", "Nelgra",
        "Nendra", "Nornessa", "Oldra", "Orlissa", "Rilda", "Sarlda", "Sigrin", "Thalra", "Thindra", "Ylissa"
    ];

    public static readonly string[] DwarfLastNames =
    [
        "Amberforge", "Anvilmar", "Battlehammer", "Blackdelve", "Blackforge", "Bloodanvil", "Boulderfall", "Brightdelve", "Broadshield", "Bronzebeard",
        "Copperbeard", "Deepdelver", "Deeppick", "Embermantle", "Fellstone", "Fireforge", "Flintshoulder", "Frostbeard", "Gembreaker", "Goldmantle",
        "Granitefist", "Granitehelm", "Gravelbeard", "Hammerdeep", "Hammerfall", "Hardsunder", "Hearthbreaker", "Ironanvil", "Ironfist", "Ironsunder",
        "Mithralsong", "Oakenshield", "Orethane", "Redboulder", "Rimehelm", "Rockarm", "Runehammer", "Shatterpick", "Shieldthane", "Silvervein",
        "Steelhelm", "Stonebeard", "Stoneforge", "Stoneshield", "Strongaxe", "Strongbreak", "Thunderhammer", "Underbarr", "Wintermantle", "Worldcarver"
    ];

    // Gnome Names
    public static readonly string[] GnomeMaleFirstNames =
    [
        "Albin", "Albus", "Baffin", "Balvin", "Berrick", "Bimfar", "Bimlor", "Bramlen", "Brannik", "Brendel",
        "Brimble", "Brinnic", "Brottel", "Calbin", "Callabyr", "Celmik", "Cendrol", "Chibbin", "Cobal", "Coddil",
        "Colbin", "Cormik", "Corvel", "Dabbin", "Dalkin", "Darnic", "Delbin", "Dimbur", "Dimmen", "Dimwick",
        "Dorrin", "Dovren", "Drimble", "Drinnoc", "Ebben", "Elmin", "Erbick", "Erivol", "Faldin", "Fellip",
        "Fenbik", "Fendrel", "Fennoc", "Figral", "Figwick", "Filbin", "Fillik", "Fimlor", "Fomble", "Gamlen",
        "Garrib", "Gelbin", "Gembry", "Gimlor", "Gimwick", "Gonril", "Gramble", "Grendic", "Gribbin", "Grimlor",
        "Havin", "Heddar", "Hendril", "Hibbin", "Himlor", "Ibben", "Iggel", "Illik", "Jalbin", "Jembly",
        "Jorwick", "Kelbin", "Kelvick", "Kendril", "Kibben", "Kimbel", "Kimmic", "Korben", "Lembin", "Lendrick",
        "Limmoc", "Mabbin", "Merrig", "Mimlor", "Mindle", "Mornik", "Nabbin", "Nendoc", "Niblen", "Nimrick",
        "Oddril", "Osrick", "Pellin", "Pimlor", "Quindle", "Rendic", "Sibbin", "Tobble", "Whindle", "Zimlor"
    ];

    public static readonly string[] GnomeFemaleFirstNames =
    [
        "Almira", "Almyl", "Banna", "Berryl", "Belbelle", "Bimni", "Binkly", "Bramelle", "Brissa", "Brivella",
        "Callie", "Celmira", "Cendie", "Chilla", "Cibelle", "Cillia", "Cindra", "Coggle", "Colbi", "Curla",
        "Dallie", "Danimy", "Darra", "Daz", "Delbelle", "Delmi", "Dimbelle", "Dimni", "Doriella", "Dorrella",
        "Drimni", "Ebella", "Effina", "Elbette", "Ellimi", "Elminy", "Emmilla", "Enni", "Ermyl", "Falla",
        "Fenna", "Fennella", "Feyni", "Figmi", "Filbelle", "Fimri", "Fimwy", "Finli", "Folni", "Galmira",
        "Gemmia", "Gembelle", "Gilla", "Gimwy", "Ginla", "Glindri", "Grimni", "Grissa", "Gwylli", "Hanni",
        "Heddra", "Helna", "Hivri", "Ibella", "Iffri", "Illka", "Imbali", "Jalmina", "Jassa", "Jembella",
        "Jinxie", "Kallin", "Kelbelle", "Kelmi", "Kimmia", "Kivri", "Lallie", "Lemmi", "Limmia", "Lindle",
        "Lynnie", "Mabbelle", "Mella", "Merryl", "Minli", "Miriella", "Nabri", "Nella", "Nibella", "Nimwy",
        "Olla", "Ormilla", "Pella", "Pimmia", "Rella", "Sella", "Talla", "Timmi", "Wimla", "Zella"
    ];

    public static readonly string[] GnomeLastNames =
    [
        "Barrellock", "Bellowbrace", "Blazegauge", "Bramblewhistle", "Brassgleam", "Brightcog", "Cappelcyll", "Cinderlace", "Copperpin", "Cowlbrass",
        "Daubspinner", "Dapplegear", "Deepgleam", "Dimwright", "Farforge", "Fizzlespark", "Flangebright", "Gearfellow", "Glimmerbolt", "Glintpedal",
        "Goldwrench", "Hammerlink", "Ironcoupler", "Janglebrace", "Kegspinner", "Latchwhistle", "Lightlever", "Lockbrass", "Mettlewhirl", "Nimwicket",
        "Pinchspanner", "Quickshaft", "Rattleweld", "Rivetwhirl", "Silverratchet", "Smallbarrel", "Sparkglove", "Springbrace", "Steamtumble", "Stonelever",
        "Tinkershod", "Tumblefiddle", "Twillclock", "Whistlelock", "Wickergleam", "Widgetbar", "Winderflange", "Wirelace", "Wrenchbottle", "Zindlebrass"
    ];

    // Half-Elf Names
    public static readonly string[] HalfElfMaleFirstNames =
    [
        "Aelar", "Aerin", "Aldriel", "Alen", "Alric", "Amarion", "Andren", "Aradan", "Aramis", "Arden",
        "Aric", "Arlen", "Arren", "Balen", "Belion", "Berris", "Brennar", "Brynion", "Caelan", "Caelor",
        "Calen", "Calyon", "Caris", "Carys", "Cathan", "Celor", "Cendric", "Cerin", "Corin", "Corven",
        "Dalen", "Darin", "Darius", "Darrion", "Davorin", "Delian", "Drevan", "Elar", "Eldren", "Elion",
        "Ellisar", "Emric", "Eran", "Eravel", "Eril", "Eron", "Evran", "Faelar", "Faelon", "Faren",
        "Faris", "Fenarion", "Fenric", "Galen", "Garrion", "Gavinor", "Halion", "Haran", "Haris", "Isandor",
        "Isen", "Jalen", "Jareth", "Jerin", "Kaelor", "Kaelric", "Kethen", "Kieran", "Korin", "Laren",
        "Larion", "Leoric", "Lethan", "Lorian", "Maelor", "Maerion", "Mairen", "Marion", "Merrin", "Nalen",
        "Narion", "Neric", "Orin", "Peren", "Quarion", "Ralen", "Renlor", "Rilian", "Saren", "Selion",
        "Taren", "Tavian", "Theron", "Torlen", "Valen", "Verras", "Veylor", "Waelan", "Zanric", "Zorin"
    ];

    public static readonly string[] HalfElfFemaleFirstNames =
    [
        "Aela", "Aelene", "Aerin", "Aeryn", "Aila", "Alenya", "Alira", "Amaera", "Amaris", "Anara",
        "Aneira", "Araleen", "Aranel", "Ariae", "Arinelle", "Arissa", "Ariya", "Avelin", "Averis", "Baela",
        "Belara", "Brenelle", "Briala", "Caelia", "Caelira", "Calia", "Calira", "Calyn", "Cariel", "Caryn",
        "Celara", "Celyra", "Ceria", "Cirenna", "Daela", "Daelin", "Dalia", "Danira", "Delara", "Delin",
        "Elaria", "Elenna", "Elira", "Ellana", "Ellera", "Ellinor", "Ellyra", "Elora", "Emara", "Emelyn",
        "Enara", "Enissa", "Erala", "Erelle", "Evalin", "Falenya", "Fariel", "Felyra", "Galira", "Galenne",
        "Gwenna", "Gwylla", "Halene", "Helira", "Ilaria", "Ilena", "Imara", "Inara", "Irissa", "Isanna",
        "Isara", "Jalira", "Jessa", "Kaelira", "Kariel", "Katria", "Kelira", "Kerissa", "Laira", "Laeriel",
        "Larena", "Lethira", "Lirae", "Lissa", "Maelin", "Maeryn", "Mariel", "Melara", "Merisse", "Naelin",
        "Nalira", "Nelisse", "Neraya", "Orelle", "Raelin", "Sariel", "Selanne", "Talia", "Vellis", "Ylanna"
    ];

    public static readonly string[] HalfElfLastNames =
    [
        "Amberwell", "Ashenford", "Barrowsong", "Blackdale", "Brightwillow", "Brookridge", "Cindervale", "Dawnwhisper", "Deepford", "Duskbrook",
        "Elmshade", "Evenwood", "Fairwind", "Fallowmere", "Ferncrest", "Galeholt", "Grayhollow", "Greenfold", "Halewyn", "Hartbrook",
        "Highfield", "Holloway", "Ironwell", "Kelridge", "Leaftide", "Lightmere", "Lonespring", "Lowriver", "Moorcrest", "Moorfield",
        "Moonmeadow", "Norhill", "Oakshade", "Ravenshore", "Redwyn", "Ridgewell", "Riverhollow", "Rowanford", "Silvershade", "Southwind",
        "Starfield", "Stonehollow", "Sunmeadow", "Thornwall", "Valecrest", "Varrowind", "Westfall", "Whitereach", "Willowmere", "Windell"
    ];

    // Halfling Names
    public static readonly string[] HalflingMaleFirstNames =
    [
        "Acorn", "Applecot", "Ashbel", "Barley", "Barrowly", "Basil", "Beetle", "Berrywick", "Bilberry", "Bimmie",
        "Bramble", "Bramlet", "Bramwell", "Brenley", "Briswick", "Brottin", "Buckthorn", "Bumble", "Burdock", "Burlap",
        "Butterwick", "Cabbageleaf", "Candlewick", "Caraway", "Cider", "Cinnamon", "Clary", "Claypot", "Clove", "Copperleaf",
        "Crispen", "Cricket", "Crumble", "Dillie", "Downburr", "Dovewell", "Drift", "Dunnet", "Elmer", "Emberlin",
        "Fallow", "Farro", "Fennel", "Fernwick", "Figbottom", "Figgins", "Fitz", "Fosco", "Frostwick", "Gadberry",
        "Gooseberry", "Gravel", "Greenshore", "Hearthwick", "Hedgewise", "Honeywick", "Hopps", "Huckle", "Juneberry", "Kettle",
        "Lintel", "Loban", "Lobell", "Loam", "Maple", "Marrow", "Meadowson", "Merryweather", "Moss", "Mosswick",
        "Mulberry", "Mumblethorn", "Murmur", "Nutkin", "Oakum", "Oatwell", "Patchy", "Peachwell", "Perriwig", "Pip",
        "Plumfield", "Pripp", "Quince", "Reed", "Rowan", "Rustle", "Sprig", "Stalk", "Straw", "Tater",
        "Thimbel", "Thistle", "Thornet", "Tumble", "Wheatley", "Whimble", "Willowby", "Wrenwick", "Zennie", "Zoffa"
    ];

    public static readonly string[] HalflingFemaleFirstNames =
    [
        "Almond", "Appleblossom", "Apria", "Aster", "Basilia", "Beanie", "Berries", "Betony", "Bloom", "Blossom",
        "Brandybloom", "Brindleleaf", "Brioche", "Briony", "Buttercup", "Candle", "Caramel", "Chamomile", "Cherry", "Chive",
        "Clover", "Cocoa", "Coriander", "Cricket", "Crumpet", "Daffodil", "Daisy", "Dandelion", "Dandy", "Dewey",
        "Dilly", "Dove", "Dovewing", "Edelberry", "Eglantine", "Emberdell", "Fanny", "Fawn", "Figsy", "Flicker",
        "Flossie", "Floweret", "Freesia", "Ginger", "Gingersnap", "Glorie", "Goldenrod", "Hazel", "Heather", "Hollyhock",
        "Honeybell", "Hyacinth", "Inkwren", "Ivy", "Juniper", "Lavender", "Leafy", "Lemonette", "Lettie", "Lily",
        "Lilac", "Linen", "Looma", "Mae", "Magnolia", "Maple", "Marigold", "Marybelle", "Meadowlark", "Merryblossom",
        "Minty", "Mistral", "Mothwing", "Mulberry", "Nettle", "Nutmeg", "Oatbelle", "Olive", "Pansy", "Peach",
        "Pearl", "Pennybright", "Pepper", "Petal", "Pinella", "Plum", "Poppy", "Primrose", "Pumpkin", "Rosebay",
        "Rosethorn", "Saffria", "Sorrel", "Strawberry", "Sunhoney", "Sunny", "Thimble", "Tilly", "Willow", "Wisteria"
    ];

    public static readonly string[] HalflingLastNames =
    [
        "Appleblossom", "Baldorf", "Berrybluff", "Birchbottle", "Bluepetal", "Brambletoe", "Brightbriar", "Caskhollow", "Cobcruncher", "Cookwillow",
        "Dapplemeadow", "Dewbreeze", "Dovewhistle", "Dustyfield", "Elderburr", "Fernhollow", "Fiddlebrook", "Figglepath", "Froghill", "Greenridge",
        "Greenspan", "Hearthbloom", "Hilldancer", "Honeywell", "Keenbarrow", "Lightstep", "Littlefoot", "Meadowrun", "Mossgather", "Oakburrow",
        "Oldfur", "Quickhollow", "Reedbottle", "Rosemead", "Rumblebrook", "Shadowbend", "Softwhistle", "Springbarrel", "Sweetbriar", "Tallbarley",
        "Torrowfire", "Tumbleleaf", "Warmbottle", "Weatherbee", "Whitseepockie", "Willowbend", "Windmeadow", "Woodbriar", "Yellowfeather", "Yonderbrook"
    ];

    // Half-Orc Names
    public static readonly string[] HalfOrcMaleFirstNames =
    [
        "Adgar", "Agran", "Aldon", "Algar", "Ardan", "Argrim", "Arvik", "Baldric", "Bannor", "Bargrim",
        "Barik", "Barton", "Beldan", "Berric", "Borven", "Brannic", "Brask", "Brondar", "Brunden", "Caldor",
        "Calgar", "Carruk", "Carven", "Cendrik", "Corgan", "Corlan", "Dalgar", "Darmik", "Darron", "Davor",
        "Delgar", "Dennar", "Drogan", "Drumm", "Durik", "Durnan", "Durric", "Eldgar", "Eldrun", "Erdan",
        "Falkor", "Farnan", "Farrik", "Felgar", "Fendran", "Fennoc", "Ferrar", "Galdron", "Garik", "Garrun",
        "Gavrik", "Gendran", "Gorvan", "Granth", "Grendar", "Grimlor", "Haldren", "Halvar", "Hannic", "Harrek",
        "Haskor", "Ildric", "Jandrek", "Jarrek", "Jorvan", "Kaldrun", "Karnak", "Keldran", "Kelric", "Kendram",
        "Korlan", "Korrin", "Largan", "Larrik", "Legrin", "Mardak", "Marlon", "Medran", "Morvran", "Narrek",
        "Neldor", "Ordric", "Orlen", "Parvik", "Ragnak", "Rasken", "Redgar", "Relvar", "Rendak", "Sarven",
        "Taldor", "Tharn", "Torvik", "Tuskan", "Uldran", "Varnek", "Varron", "Zandor", "Zarrik", "Zorvan"
    ];

    public static readonly string[] HalfOrcFemaleFirstNames =
    [
        "Aldira", "Algrin", "Arala", "Ardena", "Arvella", "Basma", "Belka", "Berrin", "Branira", "Braska",
        "Brisla", "Brunna", "Calda", "Callira", "Carrin", "Cendra", "Cendryl", "Ceralda", "Dalmira", "Dandra",
        "Dareth", "Darra", "Delka", "Dendris", "Dranna", "Draska", "Drella", "Eldra", "Elka", "Elrissa",
        "Emdara", "Falka", "Farla", "Farris", "Fennara", "Ferla", "Galdira", "Ganna", "Garrin", "Garrisa",
        "Gavella", "Gendris", "Geralda", "Gralyn", "Grenda", "Grinna", "Halra", "Hanneth", "Harnessa", "Haskira",
        "Helna", "Hendira", "Ilgra", "Inessa", "Irdra", "Jarissa", "Jaska", "Jendrin", "Kaldira", "Kalra",
        "Karnessa", "Kelra", "Kemla", "Kendra", "Korissa", "Korrin", "Landra", "Larika", "Laska", "Lendris",
        "Lerrin", "Marla", "Marnessa", "Medra", "Meldra", "Merra", "Moira", "Morla", "Nadrek", "Nalla",
        "Nandra", "Nelka", "Neressa", "Odrin", "Olgra", "Orlissa", "Parra", "Raldra", "Raska", "Saldra",
        "Sarika", "Tendra", "Thalisa", "Ulna", "Valdra", "Varlissa", "Wendra", "Yalra", "Zendra", "Zerissa"
    ];

    public static readonly string[] HalfOrcLastNames =
    [
        "Bittermark", "Blackrime", "Bloodrime", "Breakstone", "Chillscar", "Darkharrow", "Dreadmere", "Fellstar", "Gauntrock", "Grimshore",
        "Grindfell", "Hardscar", "Iceforge", "Ironrime", "Keelrath", "Nightbreaker", "Nightscar", "Rimeclaw", "Rimeshackle", "Rockmaw",
        "Sleetfell", "Snowgarde", "Snowriven", "Steelfrost", "Stormfell", "Stormgrim", "Tarnblood", "Wintercleft", "Woecliff", "Wolfscar",
        "Bearward", "Blacktotem", "Emberseer", "Farsight", "Feathermark", "Fenrun", "Hornwatch", "Moonhusk", "Osterwind", "Ravenchant",
        "Ravenwander", "Runechant", "Runekeep", "Runewatcher", "Veilrunner", "Wanderspirit", "Whispermark", "Wildchant", "Windsigil", "Wolfseeker"
    ];

    // Drow Names
    public static readonly string[] DrowMaleFirstNames =
    [
        "Akhen'dar", "Ald'zrin", "Al'vorn", "Ark'hyl", "Aun'tril", "Baer'zyn", "Bal'zhun", "Bar'rak", "Bel'kryn", "Bhal'zar",
        "Braen'druk", "Bror'nyl", "Caer'dros", "Caz'irn", "Chol'drin", "Cohl'vran", "Daer'xor", "Dhaun'rak", "Dhaur'im", "Dhoz'ren",
        "Drel'morn", "Driz'val", "Dror'nath", "Elnz'ak", "Erel'khar", "Erk'zin", "Faer'andos", "Feln'rak", "Gel'dros", "Ghel'zak",
        "Ghor'ndar", "Gho'zrim", "Ghyl'drin", "Hel'kryn", "Ilz'tor", "Ilx'ryn", "Im'rath", "Inz'olar", "Jhal'kryn", "Jhal'rak",
        "Jhul'mor", "Kaer'zmyn", "Kal'drath", "Khal'zar", "Khel'zor", "Kil'noth", "Kor'vryn", "Kra'zim", "Lhal'rak", "Loth'or",
        "Lor'zryn", "Mal'drak", "Mel'drin", "Mel'thzar", "Mor'zif", "Mzin'khal", "Nal'zren", "Nar'xil", "Nha'urn", "Nhla'kyr",
        "Nyl'tor", "Olz'iran", "Orl'vras", "Phar'nak", "Qil'thor", "Quar'vorn", "Quil'zak", "Rau'khar", "Rel'zorn", "Rhy'drak",
        "Rilyn'zar", "Rith'tar", "Riz'vorn", "Sszar'ven", "Sszor'in", "Sszul'dar", "Tal'zryn", "Teln'rak", "Thae'vir", "Thal'zak",
        "Thaun'drin", "Then'rak", "Torv'zak", "Tus'lar", "Uluth'rin", "Ul'zrak", "Var'rdros", "Vel'drath", "Vel'ukin", "Vhal'xor",
        "Vhol'rak", "Vor'nil", "Xal'drik", "Xar'nor", "Xil'zorn", "Xor'lak", "Zak'ryn", "Zhael'dros", "Zhal'rak", "Zor'vyn"
    ];

    public static readonly string[] DrowFemaleFirstNames =
    [
        "Alaun'ra", "Alzi'rae", "Ark'hessa", "Aun'rae", "Bael'uth", "Bel'ssira", "Bhael'ryn", "Bha'lara", "Brae'zira", "Bryl'thae",
        "Cae'zira", "Caz'myrra", "Chal'raen", "Chan'dryl", "Chaul'ira", "Dhaer'yssa", "Dhau'neth", "Dhil'rune", "Drae'zira", "Drel'thae",
        "Dris'senrae", "Dris'zhala", "El'vrae", "Elz'risa", "Erel'ith", "Faer'zirae", "Faer'lyssa", "Feln'rae", "Fir'aenrae", "Ghaun'ira",
        "Ghil'rae", "Ghrae'lynn", "Hal'zara", "Hel'krissa", "Il'varaen", "Il'zhyra", "Im'rae", "Inz'rel", "Inz'hara", "Ir'aeza",
        "Jhael'nith", "Jhal'dyra", "Jhul'raen", "Kaer'lyss", "Khal'yntra", "Khal'yrae", "Khel'myra", "Kil'nrae", "Kis'zara", "Kor'lythae",
        "Kra'zira", "Lha'lira", "Llae'zrae", "Llor'vra", "Llu'lith", "Mal'rae", "Malz'triss", "Mel'drissa", "Mez'lara", "Mzryn'thae",
        "Nhael'yra", "Nhal'ra", "Nhil'rae", "Nhyss'ira", "Olz'reth", "Or'vraen", "Phal'ra", "Qiln'rae", "Quar'issa", "Raez'lin",
        "Rel'vra", "Rhyl'ssa", "Ril'zrae", "Roz'hira", "Sszar'en", "Sszin'dyra", "Sszyn'rae", "Szin'valae", "Ta'lirae", "Tel'nissa",
        "Thae'vel", "Thal'zira", "Thin'drae", "Tor'lyrae", "Ul'vrae", "Vae'zyra", "Vae'rissa", "Vel'kriss", "Vel'zhira", "Vhae'lyrne",
        "Vho'lira", "Vil'drae", "Xar'zyl", "Xiln'rae", "Xyr'lyssa", "Zhaer'ith", "Zhal'rae", "Zin'ryssa", "Zyl'vrae", "Zy'vreaen"
    ];

    public static readonly string[] DrowLastNames =
    [
        "Alean'thrae", "Baen'rahel", "Balyn'dris", "Chor'zyn", "Dhaun'tlar", "Din'lilth", "Drath'myr", "Dryn'valas", "El'torchlaen", "Faerz'rahel",
        "Fel'thiss", "Ghlar'aen", "Gloz'tyrr", "Hal'veris", "Hel'vrauth", "Il'lyndarr", "Ilph'raema", "Im'drys", "Khaer'dolin", "Kil'raen",
        "Lhel'quess", "Lrin'vyr", "Mal'tyrr", "Maern'yl", "Mez'drithar", "Myr'telan", "Nhal'raema", "Olon'vrae", "Or'lyndarr", "Ouss'talan",
        "Phal'raen", "Quav'rahel", "Rel'drinn", "Rhyl'tyssa", "Seld'vorn", "Shyn'tlar", "Ssam'braen", "Sszin'dril", "Tal'myrin", "Telen'vrae",
        "Ul'vrath", "Val'syndra", "Velkyn'rahl", "Ver'kharis", "Vorn'zar", "Xor'laran", "Xor'ril", "Y'vressin", "Zaer'lyn", "Zor'quess"
    ];
}



