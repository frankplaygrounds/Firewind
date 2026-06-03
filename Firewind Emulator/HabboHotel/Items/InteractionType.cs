using Firewind.Core;
using System;

namespace Firewind.HabboHotel.Items
{
    enum InteractionType
    {
        none, //None == default
        gate,
        postit,
        roomeffect,
        dimmer,
        trophy,
        bed,
        scoreboard,
        vendingmachine,
        alert,
        onewaygate,
        loveshuffler,
        habbowheel,
        dice,
        bottle,
        teleport,
        rentals,
        pet,
        pool,
        roller,
        fbgate,
        iceskates,
        normslaskates,
        lowpool,
        haloweenpool,
        football,
        footballgoalgreen,
        footballgoalyellow,
        footballgoalblue,
        footballgoalred,
        footballcountergreen,
        footballcounteryellow,
        footballcounterblue,
        footballcounterred,
        banzaigateblue,
        banzaigatered,
        banzaigateyellow,
        banzaigategreen,
        banzaifloor,
        banzaiscoreblue,
        banzaiscorered,
        banzaiscoreyellow,
        banzaiscoregreen,
        banzaicounter,
        banzaitele,
        banzaipuck,
        banzaipyramid,
        freezetimer,
        freezeexit,
        freezeredcounter,
        freezebluecounter,
        freezeyellowcounter,
        freezegreencounter,
        freezeyellowgate,
        freezeredgate,
        freezegreengate,
        freezebluegate,
        freezetileblock,
        freezetile,
        jukebox,
        musicdisc,
        puzzlebox,


        //Wired:
        triggertimer,
        triggerroomenter,
        triggergameend,
        triggergamestart,
        triggerrepeater,
        triggeronusersay,
        triggerscoreachieved,
        triggerstatechanged,
        triggerwalkonfurni,
        triggerwalkofffurni,

        actiongivescore,
        actionposreset,
        actionmoverotate,
        actionresettimer,
        actionshowmessage,
        actionteleportto,
        actiontogglestate,

        conditionfurnishaveusers,
        conditionstatepos,
        conditiontimelessthan,
        conditiontimemorethan,
        conditiontriggeronfurni,

        arrowplate,
        preassureplate,
        ringplate,
        colortile,
        colorwheel,
        floorswitch1,
        floorswitch2,
        firegate,
        glassfoor,

        specialrandom,
        specialunseen,

        wire,
        wireCenter,
        wireCorner,
        wireSplitter,
        wireStandard,

        gift,
        mannequin,
        guildgeneric,
        guilddoor
    }

    class InterractionTypes
    {
        internal static InteractionType GetTypeFromString(string pType)
        {
            if (string.IsNullOrWhiteSpace(pType))
                return InteractionType.none;

            pType = pType.Trim().ToLowerInvariant();
            if (pType.StartsWith("pet", StringComparison.OrdinalIgnoreCase) && pType.Length > 3)
            {
                int petType;
                if (int.TryParse(pType.Substring(3), out petType))
                    return InteractionType.pet;
            }

            switch (pType)
            {
                case "":
                case "default":
                case "normal":
                    return InteractionType.none;
                case "gate":
                case "club_gate":
                    return InteractionType.gate;
                case "postit":
                    return InteractionType.postit;
                case "roomeffect":
                case "effect_tile":
                case "fx_box":
                case "tile_fxprovider_nfs":
                    return InteractionType.roomeffect;
                case "dimmer":
                case "background_toner":
                    return InteractionType.dimmer;
                case "trophy":
                    return InteractionType.trophy;
                case "bed":
                    return InteractionType.bed;
                case "scoreboard":
                case "vote_counter":
                case "wf_highscore":
                    return InteractionType.scoreboard;
                case "vendingmachine":
                    return InteractionType.vendingmachine;
                case "alert":
                    return InteractionType.alert;
                case "onewaygate":
                    return InteractionType.onewaygate;
                case "loveshuffler":
                    return InteractionType.loveshuffler;
                case "habbowheel":
                    return InteractionType.habbowheel;
                case "dice":
                    return InteractionType.dice;
                case "bottle":
                    return InteractionType.bottle;
                case "teleport":
                case "hopper":
                case "club_hopper":
                case "costume_hoppper":
                case "teleporttile":
                    return InteractionType.teleport;
                case "rentals":
                case "rentable_space":
                    return InteractionType.rentals;
                case "pet":
                    return InteractionType.pet;
                case "pool":
                case "water":
                case "water_item":
                case "wateritem":
                    return InteractionType.pool;
                case "roller":
                    return InteractionType.roller;
                case "fbgate":
                case "football_gate":
                    return InteractionType.fbgate;
                case "iceskates":
                    return InteractionType.iceskates;
                case "rollerskate":
                case "rollerskate_field":
                    return InteractionType.normslaskates;
                case "lowpool":
                    return InteractionType.lowpool;
                case "haloweenpool":
                    return InteractionType.haloweenpool;
                case "ball":
                case "football":
                    return InteractionType.football;

                case "footballgoalgreen":
                case "green_goal":
                case "football_goal_green":
                    return InteractionType.footballgoalgreen;
                case "footballgoalyellow":
                case "yellow_goal":
                case "football_goal_yellow":
                    return InteractionType.footballgoalyellow;
                case "footballgoalred":
                case "red_goal":
                case "football_goal_red":
                    return InteractionType.footballgoalred;
                case "footballgoalblue":
                case "blue_goal":
                case "football_goal_blue":
                    return InteractionType.footballgoalblue;

                case "footballcountergreen":
                case "green_score":
                case "football_counter_green":
                    return InteractionType.footballcountergreen;
                case "footballcounteryellow":
                case "yellow_score":
                case "football_counter_yellow":
                    return InteractionType.footballcounteryellow;
                case "footballcounterblue":
                case "blue_score":
                case "football_counter_blue":
                    return InteractionType.footballcounterblue;
                case "footballcounterred":
                case "red_score":
                case "football_counter_red":
                    return InteractionType.footballcounterred;

                case "banzaigateblue":
                case "bb_blue_gate":
                case "battlebanzai_gate_blue":
                    return InteractionType.banzaigateblue;
                case "banzaigatered":
                case "bb_red_gate":
                case "battlebanzai_gate_red":
                    return InteractionType.banzaigatered;
                case "banzaigateyellow":
                case "bb_yellow_gate":
                case "battlebanzai_gate_yellow":
                    return InteractionType.banzaigateyellow;
                case "banzaigategreen":
                case "bb_green_gate":
                case "battlebanzai_gate_green":
                    return InteractionType.banzaigategreen;
                case "banzaifloor":
                case "bb_patch":
                case "battlebanzai_tile":
                    return InteractionType.banzaifloor;

                case "banzaiscoreblue":
                case "battlebanzai_counter_blue":
                    return InteractionType.banzaiscoreblue;
                case "banzaiscorered":
                case "battlebanzai_counter_red":
                    return InteractionType.banzaiscorered;
                case "banzaiscoreyellow":
                case "battlebanzai_counter_yellow":
                    return InteractionType.banzaiscoreyellow;
                case "banzaiscoregreen":
                case "battlebanzai_counter_green":
                    return InteractionType.banzaiscoregreen;

                case "banzaicounter":
                case "counter":
                case "battlebanzai_timer":
                    return InteractionType.banzaicounter;
                case "banzaitele":
                case "bb_teleport":
                case "battlebanzai_random_teleport":
                    return InteractionType.banzaitele;
                case "banzaipuck":
                case "bb_puck":
                case "battlebanzai_puck":
                    return InteractionType.banzaipuck;
                case "banzaipyramid":
                case "pyramid":
                    return InteractionType.banzaipyramid;

                case "freezetimer":
                case "game_timer":
                    return InteractionType.freezetimer;
                case "freezeexit":
                case "freeze_exit":
                    return InteractionType.freezeexit;
                case "freezeredcounter":
                case "freeze_counter_red":
                    return InteractionType.freezeredcounter;
                case "freezebluecounter":
                case "freeze_counter_blue":
                    return InteractionType.freezebluecounter;
                case "freezeyellowcounter":
                case "freeze_counter_yellow":
                    return InteractionType.freezeyellowcounter;
                case "freezegreencounter":
                case "freeze_counter_green":
                    return InteractionType.freezegreencounter;
                case "freezeyellowgate":
                case "freeze_gate_yellow":
                    return InteractionType.freezeyellowgate;
                case "freezeredgate":
                case "freeze_gate_red":
                    return InteractionType.freezeredgate;
                case "freezegreengate":
                case "freeze_gate_green":
                    return InteractionType.freezegreengate;
                case "freezebluegate":
                case "freeze_gate_blue":
                    return InteractionType.freezebluegate;
                case "freezetileblock":
                case "freeze_block":
                    return InteractionType.freezetileblock;
                case "freezetile":
                case "freeze_tile":
                    return InteractionType.freezetile;
                case "jukebox":
                case "trax_machine":
                    return InteractionType.jukebox;
                case "musicdisc":
                case "sound_fx":
                    return InteractionType.musicdisc;

                case "triggertimer":
                case "wf_trg_attime":
                case "wf_trg_at_given_time":
                case "wf_trg_at_time_long":
                    return InteractionType.triggertimer;
                case "triggerroomenter":
                case "wf_trg_enterroom":
                case "wf_trg_enter_room":
                    return InteractionType.triggerroomenter;
                case "triggergameend":
                case "wf_trg_gameend":
                case "wf_trg_game_ends":
                    return InteractionType.triggergameend;
                case "triggergamestart":
                case "wf_trg_gamestart":
                case "wf_trg_game_starts":
                    return InteractionType.triggergamestart;
                case "triggerrepeater":
                case "wf_trg_timer":
                case "wf_trg_periodically":
                case "wf_trg_period_long":
                    return InteractionType.triggerrepeater;
                case "triggeronusersay":
                case "wf_trg_onsay":
                case "wf_trg_says_something":
                    return InteractionType.triggeronusersay;
                case "triggerscoreachieved":
                case "wf_trg_atscore":
                case "wf_trg_score_achieved":
                    return InteractionType.triggerscoreachieved;
                case "triggerstatechanged":
                case "wf_trg_furnistate":
                case "wf_trg_state_changed":
                    return InteractionType.triggerstatechanged;
                case "triggerwalkonfurni":
                case "wf_trg_onfurni":
                case "wf_trg_walks_on_furni":
                    return InteractionType.triggerwalkonfurni;
                case "triggerwalkofffurni":
                case "wf_trg_offfurni":
                case "wf_trg_walks_off_furni":
                    return InteractionType.triggerwalkofffurni;
                case "actiongivescore":
                case "wf_act_givepoints":
                case "wf_act_give_score":
                case "wf_act_give_score_tm":
                    return InteractionType.actiongivescore;
                case "actionposreset":
                case "wf_act_matchfurni":
                case "wf_act_match_to_sshot":
                    return InteractionType.actionposreset;
                case "actionmoverotate":
                case "wf_act_moverotate":
                case "wf_act_move_rotate":
                case "wf_act_move_furni_to":
                case "wf_act_move_to_dir":
                    return InteractionType.actionmoverotate;
                case "actionresettimer":
                case "wf_act_reset_timers":
                    return InteractionType.actionresettimer;
                case "actionshowmessage":
                case "wf_act_saymsg":
                case "wf_act_show_message":
                    return InteractionType.actionshowmessage;
                case "actionteleportto":
                case "wf_act_moveuser":
                case "wf_act_teleport_to":
                    return InteractionType.actionteleportto;
                case "actiontogglestate":
                case "wf_act_togglefurni":
                case "wf_act_toggle_state":
                case "wf_act_toggle_to_rnd":
                    return InteractionType.actiontogglestate;
                case "conditionfurnishaveusers":
                case "wf_cnd_furnis_hv_avtrs":
                    return InteractionType.conditionfurnishaveusers;
                case "conditionstatepos":
                case "wf_cnd_match_snapshot":
                case "wf_cnd_not_match_snap":
                case "wf_cnd_stuff_is":
                case "wf_cnd_not_stuff_is":
                    return InteractionType.conditionstatepos;
                case "conditiontimelessthan":
                case "wf_cnd_time_less_than":
                    return InteractionType.conditiontimelessthan;
                case "conditiontimemorethan":
                case "wf_cnd_time_more_than":
                    return InteractionType.conditiontimemorethan;
                case "conditiontriggeronfurni":
                case "wf_cnd_trggrer_on_frn":
                case "wf_cnd_not_trggrer_on":
                    return InteractionType.conditiontriggeronfurni;
                case "arrowplate":
                    return InteractionType.arrowplate;
                case "preassureplate":
                case "pressureplate":
                case "pressureplate_group":
                    return InteractionType.preassureplate;
                case "ringplate":
                    return InteractionType.ringplate;
                case "colortile":
                case "wf_colortile":
                    return InteractionType.colortile;
                case "colorwheel":
                    return InteractionType.colorwheel;
                case "floorswitch1":
                case "floor_switch":
                case "switch":
                case "wf_floor_switch1":
                    return InteractionType.floorswitch1;
                case "floorswitch2":
                case "wf_floor_switch2":
                    return InteractionType.floorswitch2;
                case "firegate":
                case "wf_firegate":
                    return InteractionType.firegate;
                case "glassfoor":
                    return InteractionType.glassfoor;
                case "specialrandom":
                case "wf_xtra_random":
                    return InteractionType.specialrandom;
                case "specialunseen":
                case "wf_xtra_unseen":
                    return InteractionType.specialunseen;
                case "wire":
                    return InteractionType.wire;
                case "wirecenter":
                case "wire_center":
                    return InteractionType.wireCenter;
                case "wirecorner":
                case "wire_corner":
                    return InteractionType.wireCorner;
                case "wiresplitter":
                case "wire_splitter":
                    return InteractionType.wireSplitter;
                case "wirestandard":
                case "wire_standard":
                    return InteractionType.wireStandard;
                case "puzzlebox":
                case "puzzle_box":
                    return InteractionType.puzzlebox;
                case "gift":
                    return InteractionType.gift;
                case "mannequin":
                    return InteractionType.mannequin;
                case "guild_item":
                case "gld_item":
                case "guild_furni":
                case "guildgeneric":
                    return InteractionType.guildgeneric;
                case "guild_gate":
                case "gld_gate":
                case "guilddoor":
                    return InteractionType.guilddoor;
                //case "":
                //case "default":
                //    return InteractionType.none;
                //case "gate":
                //    return InteractionType.gate;
                //case "postit":
                //    return InteractionType.postit;
                //case "roomeffect":
                //    return InteractionType.roomeffect;
                //case "dimmer":
                //    return InteractionType.dimmer;
                //case "trophy":
                //    return InteractionType.trophy;
                //case "bed":
                //    return InteractionType.bed;
                //case "scoreboard":
                //    return InteractionType.scoreboard;
                //case "vendingmachine":
                //    return InteractionType.vendingmachine;
                //case "alert":
                //    return InteractionType.alert;
                //case "onewaygate":
                //    return InteractionType.onewaygate;
                //case "loveshuffler":
                //    return InteractionType.loveshuffler;
                //case "habbowheel":
                //    return InteractionType.habbowheel;
                //case "dice":
                //    return InteractionType.dice;
                //case "bottle":
                //    return InteractionType.bottle;
                //case "teleport":
                //    return InteractionType.teleport;
                //case "rentals":
                //    return InteractionType.rentals;
                //case "pet":
                //    return InteractionType.pet;
                //case "pool":
                //    return InteractionType.pool;
                //case "roller":
                //    return InteractionType.roller;
                //case "fbgate":
                //    return InteractionType.fbgate;
                //case "pet0":
                //    return InteractionType.pet0;
                //case "pet1":
                //    return InteractionType.pet1;
                //case "pet2":
                //    return InteractionType.pet2;
                //case "pet3":
                //    return InteractionType.pet3;
                //case "pet4":
                //    return InteractionType.pet4;
                //case "pet5":
                //    return InteractionType.pet5;
                //case "pet6":
                //    return InteractionType.pet6;
                //case "pet7":
                //    return InteractionType.pet7;
                //case "pet8":
                //    return InteractionType.pet8;
                //case "pet9":
                //    return InteractionType.pet9;
                //case "pet10":
                //    return InteractionType.pet10;
                //case "pet11":
                //    return InteractionType.pet11;
                //case "pet12":
                //    return InteractionType.pet12;
                //case "pet13": // Caballo
                //    return InteractionType.pet13;
                //case "pet14":
                //    return InteractionType.pet14;
                //case "pet15":
                //    return InteractionType.pet15;
                //case "pet16": // Mascota agregada
                //    return InteractionType.pet16;
                //case "pet17": // Mascota agregada
                //    return InteractionType.pet17;
                //case "pet18": // Mascota agregada
                //    return InteractionType.pet18;
                //case "pet19": // Mascota agregada
                //    return InteractionType.pet19;
                //case "pet20": // Mascota agregada
                //    return InteractionType.pet20;
                //case "pet21": // Mascota agregada
                //    return InteractionType.pet21;
                //case "pet22": // Mascota agregada
                //    return InteractionType.pet22;
                //case "iceskates":
                //    return InteractionType.iceskates;
                //case "normalskates":
                //    return InteractionType.normslaskates;
                //case "lowpool":
                //    return InteractionType.lowpool;
                //case "haloweenpool":
                //    return InteractionType.haloweenpool;
                //case "football":
                //    return InteractionType.football;

                //case "footballgoalgreen":
                //    return InteractionType.footballgoalgreen;
                //case "footballgoalyellow":
                //    return InteractionType.footballgoalyellow;
                //case "footballgoalred":
                //    return InteractionType.footballgoalred;
                //case "footballgoalblue":
                //    return InteractionType.footballgoalblue;

                //case "footballcountergreen":
                //    return InteractionType.footballcountergreen;
                //case "footballcounteryellow":
                //    return InteractionType.footballcounteryellow;
                //case "footballcounterblue":
                //    return InteractionType.footballcounterblue;
                //case "footballcountered":
                //    return InteractionType.footballcounterred;

                //case "banzaigateblue":
                //    return InteractionType.banzaigateblue;
                //case "banzaigatered":
                //    return InteractionType.banzaigatered;
                //case "banzaigateyellow":
                //    return InteractionType.banzaigateyellow;
                //case "banzaigategreen":
                //    return InteractionType.banzaigategreen;
                //case "banzaifloor":
                //    return InteractionType.banzaifloor;

                //case "banzaiscoreblue":
                //    return InteractionType.banzaiscoreblue;
                //case "banzaiscorered":
                //    return InteractionType.banzaiscorered;
                //case "banzaiscoreyellow":
                //    return InteractionType.banzaiscoreyellow;
                //case "banzaiscoregreen":
                //    return InteractionType.banzaiscoregreen;

                //case "banzaicounter":
                //    return InteractionType.banzaicounter;
                //case "banzaitele":
                //    return InteractionType.banzaitele;
                //case "banzaipuck":
                //    return InteractionType.banzaipuck;
                //case "banzaipyramid":
                //    return InteractionType.banzaipyramid;

                //case "freezetimer":
                //    return InteractionType.freezetimer;
                //case "freezeexit":
                //    return InteractionType.freezeexit;
                //case "freezeredcounter":
                //    return InteractionType.freezeredcounter;
                //case "freezebluecounter":
                //    return InteractionType.freezebluecounter;
                //case "freezeyellowcounter":
                //    return InteractionType.freezeyellowcounter;
                //case "freezegreencounter":
                //    return InteractionType.freezegreencounter;
                //case "freezeyellowgate":
                //    return InteractionType.freezeyellowgate;
                //case "freezeredgate":
                //    return InteractionType.freezeredgate;
                //case "freezegreengate":
                //    return InteractionType.freezegreengate;
                //case "freezebluegate":
                //    return InteractionType.freezebluegate;
                //case "freezetileblock":
                //    return InteractionType.freezetileblock;
                //case "freezetile":
                //    return InteractionType.freezetile;
                //case "jukebox":
                //    return InteractionType.jukebox;
                //case "musicdisc":
                //    return InteractionType.musicdisc;

                //case "triggertimer":
                //    return InteractionType.triggertimer;
                //case "triggerroomenter":
                //    return InteractionType.triggerroomenter;
                //case "triggergameend":
                //    return InteractionType.triggergameend;
                //case "triggergamestart":
                //    return InteractionType.triggergamestart;
                //case "triggerrepeater":
                //    return InteractionType.triggerrepeater;
                //case "triggeronusersay":
                //    return InteractionType.triggeronusersay;
                //case "triggerscoreachieved":
                //    return InteractionType.triggerscoreachieved;
                //case "triggerstatechanged":
                //    return InteractionType.triggerstatechanged;
                //case "triggerwalkonfurni":
                //    return InteractionType.triggerwalkonfurni;
                //case "triggerwalkofffurni":
                //    return InteractionType.triggerwalkofffurni;
                //case "actiongivescore":
                //    return InteractionType.actiongivescore;
                //case "actionposreset":
                //    return InteractionType.actionposreset;
                //case "actionmoverotate":
                //    return InteractionType.actionmoverotate;
                //case "actionresettimer":
                //    return InteractionType.actionresettimer;
                //case "actionshowmessage":
                //    return InteractionType.actionshowmessage;
                //case "actionteleportto":
                //    return InteractionType.actionteleportto;
                //case "actiontogglestate":
                //    return InteractionType.actiontogglestate;
                //case "conditionfurnishaveusers":
                //    return InteractionType.conditionfurnishaveusers;
                //case "conditionstatepos":
                //    return InteractionType.conditionstatepos;
                //case "conditiontimelessthan":
                //    return InteractionType.conditiontimelessthan;
                //case "conditiontimemorethan":
                //    return InteractionType.conditiontimemorethan;
                //case "conditiontriggeronfurni":
                //    return InteractionType.conditiontriggeronfurni;
                //case "arrowplate":
                //    return InteractionType.arrowplate;
                //case "preassureplate":
                //    return InteractionType.preassureplate;
                //case "ringplate":
                //    return InteractionType.ringplate;
                //case "colortile":
                //    return InteractionType.colortile;
                //case "colorwheel":
                //    return InteractionType.colorwheel;
                //case "floorswitch1":
                //    return InteractionType.floorswitch1;
                //case "floorswitch2":
                //    return InteractionType.floorswitch2;
                //case "firegate":
                //    return InteractionType.firegate;
                //case "glassfoor":
                //    return InteractionType.glassfoor;
                //case "specialrandom":
                //    return InteractionType.specialrandom;
                //case "specialunseen":
                //    return InteractionType.specialunseen;
                //case "wire":
                //    return InteractionType.wire;
                //case "wireCenter":
                //    return InteractionType.wireCenter;
                //case "wireCorner":
                //    return InteractionType.wireCorner;
                //case "wireSplitter":
                //    return InteractionType.wireSplitter;
                //case "wireStandard":
                //    return InteractionType.wireStandard;
                //case "puzzlebox":
                //    return InteractionType.puzzlebox;
                //case "gift":
                //    return InteractionType.gift;
                default:
                    {

                        //Logging.WriteLine("Unknown interaction type in parse code: " + pType);
                        return InteractionType.none;
                    }
            }
        }

        internal static string ToString(InteractionType pType)
        {
            switch (pType)
            {
                case InteractionType.none:
                    return "default";
                case InteractionType.gate:
                    return "gate";
                case InteractionType.postit:
                    return "postit";
                case InteractionType.roomeffect:
                    return "roomeffect";
                case InteractionType.dimmer:
                    return "dimmer";
                case InteractionType.trophy:
                    return "trophy";
                case InteractionType.bed:
                    return "bed";
                case InteractionType.scoreboard:
                    return "scoreboard";
                case InteractionType.vendingmachine:
                    return "vendingmachine";
                case InteractionType.alert:
                    return "alert";
                case InteractionType.onewaygate:
                    return "onewaygate";
                case InteractionType.loveshuffler:
                    return "loveshuffler";
                case InteractionType.habbowheel:
                    return "habbowheel";
                case InteractionType.dice:
                    return "dice";
                case InteractionType.bottle:
                    return "bottle";
                case InteractionType.teleport:
                    return "teleport";
                case InteractionType.rentals:
                    return "rentals";
                case InteractionType.pet:
                    return "pet";
                case InteractionType.pool:
                    return "pool";
                case InteractionType.roller:
                    return "roller";
                case InteractionType.fbgate:
                    return "fbgate";
                case InteractionType.iceskates:
                    return "iceskates";
                case InteractionType.normslaskates:
                    return "normalskates";
                case InteractionType.lowpool:
                    return "lowpool";
                case InteractionType.haloweenpool:
                    return "haloweenpool";
                case InteractionType.football:
                    return "football";

                case InteractionType.footballgoalgreen:
                    return "footballgoalgreen";
                case InteractionType.footballgoalyellow:
                    return "footballgoalyellow";
                case InteractionType.footballgoalred:
                    return "footballgoalred";
                case InteractionType.footballgoalblue:
                    return "footballgoalblue";

                case InteractionType.footballcountergreen:
                    return "footballcountergreen";
                case InteractionType.footballcounteryellow:
                    return "footballcounteryellow";
                case InteractionType.footballcounterblue:
                    return "footballcounterblue";
                case InteractionType.footballcounterred:
                    return "footballcountered";

                case InteractionType.banzaigateblue:
                    return "banzaigateblue";
                case InteractionType.banzaigatered:
                    return "banzaigatered";
                case InteractionType.banzaigateyellow:
                    return "banzaigateyellow";
                case InteractionType.banzaigategreen:
                    return "banzaigategreen";
                case InteractionType.banzaifloor:
                    return "banzaifloor";

                case InteractionType.banzaiscoreblue:
                    return "banzaiscoreblue";
                case InteractionType.banzaiscorered:
                    return "banzaiscorered";
                case InteractionType.banzaiscoreyellow:
                    return "banzaiscoreyellow";
                case InteractionType.banzaiscoregreen:
                    return "banzaiscoregreen";
                case InteractionType.banzaicounter:
                    return "banzaicounter";
                case InteractionType.banzaipuck:
                    return "banzaipuck";
                case InteractionType.banzaitele:
                    return "banzaitele";
                case InteractionType.banzaipyramid:
                    return "banzaipyramid";

                case InteractionType.freezetimer:
                    return "freezetimer";
                case InteractionType.freezeexit:
                    return "freezeexit";
                case InteractionType.freezeredcounter:
                    return "freezeredcounter";
                case InteractionType.freezebluecounter:
                    return "freezebluecounter";
                case InteractionType.freezeyellowcounter:
                    return "freezeyellowcounter";
                case InteractionType.freezegreencounter:
                    return "freezegreencounter";
                case InteractionType.freezeyellowgate:
                    return "freezeyellowgate";
                case InteractionType.freezeredgate:
                    return "freezeredgate";
                case InteractionType.freezegreengate:
                    return "freezegreengate";
                case InteractionType.freezebluegate:
                    return "freezebluegate";
                case InteractionType.freezetileblock:
                    return "freezetileblock";
                case InteractionType.freezetile:
                    return "freezetile";
                case InteractionType.jukebox:
                    return "jukebox";
                case InteractionType.musicdisc:
                    return "musicdisc";

                case InteractionType.triggertimer:
                    return "triggertimer";
                case InteractionType.triggerroomenter:
                    return "triggerroomenter";
                case InteractionType.triggergameend:
                    return "triggergameend";
                case InteractionType.triggergamestart:
                    return "triggergamestart";
                case InteractionType.triggerrepeater:
                    return "triggerrepeater";
                case InteractionType.triggeronusersay:
                    return "triggeronusersay";
                case InteractionType.triggerscoreachieved:
                    return "triggerscoreachieved";
                case InteractionType.triggerstatechanged:
                    return "triggerstatechanged";
                case InteractionType.triggerwalkonfurni:
                    return "triggerwalkonfurni";
                case InteractionType.triggerwalkofffurni:
                    return "triggerwalkofffurni";
                case InteractionType.actiongivescore:
                    return "actiongivescore";
                case InteractionType.actionposreset:
                    return "actionposreset";
                case InteractionType.actionmoverotate:
                    return "actionmoverotate";
                case InteractionType.actionresettimer:
                    return "actionresettimer";
                case InteractionType.actionshowmessage:
                    return "actionshowmessage";
                case InteractionType.actionteleportto:
                    return "actionteleportto";
                case InteractionType.actiontogglestate:
                    return "actiontogglestate";
                case InteractionType.conditionfurnishaveusers:
                    return "conditionfurnishaveusers";
                case InteractionType.conditionstatepos:
                    return "conditionstatepos";
                case InteractionType.conditiontimelessthan:
                    return "conditiontimelessthan";
                case InteractionType.conditiontimemorethan:
                    return "conditiontimemorethan";
                case InteractionType.conditiontriggeronfurni:
                    return "conditiontriggeronfurni";
                case InteractionType.arrowplate:
                    return "arrowplate";
                case InteractionType.preassureplate:
                    return "preassureplate";
                case InteractionType.ringplate:
                    return "ringplate";
                case InteractionType.colortile:
                    return "colortile";
                case InteractionType.colorwheel:
                    return "colorwheel";
                case InteractionType.floorswitch1:
                    return "floorswitch1";
                case InteractionType.floorswitch2:
                    return "floorswitch2";
                case InteractionType.firegate:
                    return "firegate";
                case InteractionType.glassfoor:
                    return "glassfoor";
                case InteractionType.specialrandom:
                    return "specialrandom";
                case InteractionType.specialunseen:
                    return "specialunseen";
                case InteractionType.wire:
                    return "wire";
                case InteractionType.wireCenter:
                    return "wireCenter";
                case InteractionType.wireCorner:
                    return "wireCorner";
                case InteractionType.wireSplitter:
                    return "wireSplitter";
                case InteractionType.wireStandard:
                    return "wireStandard";
                case InteractionType.puzzlebox:
                    return "puzzlebox";
                case InteractionType.gift:
                    return "gift";
                case InteractionType.mannequin:
                    return "mannequin";
                case InteractionType.guildgeneric:
                    return "gld_item";
                case InteractionType.guilddoor:
                    return "guild_gate";
                default:
                    {
                        Logging.LogException("Unknown interaction type in to string code: " + pType);
                        return "default";
                    }
            }
        }
    }
}
