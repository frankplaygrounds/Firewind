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
            switch (pType)
            {
                case "":
                case "default":
                    return InteractionType.none;
                case "gate":
                    return InteractionType.gate;
                case "postit":
                    return InteractionType.postit;
                case "roomeffect":
                    return InteractionType.roomeffect;
                case "dimmer":
                    return InteractionType.dimmer;
                case "trophy":
                    return InteractionType.trophy;
                case "bed":
                    return InteractionType.bed;
                case "scoreboard":
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
                    return InteractionType.teleport;
                case "rentals":
                    return InteractionType.rentals;
                case "pet":
                    return InteractionType.pet;
                case "water":
                    return InteractionType.pool;
                case "roller":
                    return InteractionType.roller;
                case "fbgate":
                    return InteractionType.fbgate;
                case "iceskates":
                    return InteractionType.iceskates;
                case "rollerskate":
                    return InteractionType.normslaskates;
                case "lowpool":
                    return InteractionType.lowpool;
                case "haloweenpool":
                    return InteractionType.haloweenpool;
                case "ball":
                    return InteractionType.football;

                case "green_goal":
                    return InteractionType.footballgoalgreen;
                case "yellow_goal":
                    return InteractionType.footballgoalyellow;
                case "red_goal":
                    return InteractionType.footballgoalred;
                case "blue_goal":
                    return InteractionType.footballgoalblue;

                case "green_score":
                    return InteractionType.footballcountergreen;
                case "yellow_score":
                    return InteractionType.footballcounteryellow;
                case "blue_score":
                    return InteractionType.footballcounterblue;
                case "red_score":
                    return InteractionType.footballcounterred;

                case "bb_blue_gate":
                    return InteractionType.banzaigateblue;
                case "bb_red_gate":
                    return InteractionType.banzaigatered;
                case "bb_yellow_gate":
                    return InteractionType.banzaigateyellow;
                case "bb_green_gate":
                    return InteractionType.banzaigategreen;
                case "bb_patch":
                    return InteractionType.banzaifloor;

                case "banzaiscoreblue":
                    return InteractionType.banzaiscoreblue;
                case "banzaiscorered":
                    return InteractionType.banzaiscorered;
                case "banzaiscoreyellow":
                    return InteractionType.banzaiscoreyellow;
                case "banzaiscoregreen":
                    return InteractionType.banzaiscoregreen;

                case "counter":
                    return InteractionType.banzaicounter;
                case "bb_teleport":
                    return InteractionType.banzaitele;
                case "bb_puck":
                    return InteractionType.banzaipuck;
                case "banzaipyramid":
                    return InteractionType.banzaipyramid;

                case "freezetimer":
                    return InteractionType.freezetimer;
                case "freezeexit":
                    return InteractionType.freezeexit;
                case "freezeredcounter":
                    return InteractionType.freezeredcounter;
                case "freezebluecounter":
                    return InteractionType.freezebluecounter;
                case "freezeyellowcounter":
                    return InteractionType.freezeyellowcounter;
                case "freezegreencounter":
                    return InteractionType.freezegreencounter;
                case "freezeyellowgate":
                    return InteractionType.freezeyellowgate;
                case "freezeredgate":
                    return InteractionType.freezeredgate;
                case "freezegreengate":
                    return InteractionType.freezegreengate;
                case "freezebluegate":
                    return InteractionType.freezebluegate;
                case "freezetileblock":
                    return InteractionType.freezetileblock;
                case "freezetile":
                    return InteractionType.freezetile;
                case "jukebox":
                    return InteractionType.jukebox;
                case "musicdisc":
                    return InteractionType.musicdisc;

                case "wf_trg_attime":
                    return InteractionType.triggertimer;
                case "wf_trg_enterroom":
                    return InteractionType.triggerroomenter;
                case "wf_trg_gameend":
                    return InteractionType.triggergameend;
                case "wf_trg_gamestart":
                    return InteractionType.triggergamestart;
                case "wf_trg_timer":
                    return InteractionType.triggerrepeater;
                case "wf_trg_onsay":
                    return InteractionType.triggeronusersay;
                case "wf_trg_atscore":
                    return InteractionType.triggerscoreachieved;
                case "wf_trg_furnistate":
                    return InteractionType.triggerstatechanged;
                case "wf_trg_state_changed":
                    return InteractionType.triggerstatechanged;
                case "wf_trg_onfurni":
                case "wf_trg_walks_on_furni":
                    return InteractionType.triggerwalkonfurni;
                case "wf_trg_offfurni":
                case "wf_trg_walks_off_furni":
                    return InteractionType.triggerwalkofffurni;
                case "wf_act_givepoints":
                case "wf_act_give_score":
                    return InteractionType.actiongivescore;
                case "actionposreset":
                case "wf_act_matchfurni":
                case "wf_act_match_to_sshot":
                    return InteractionType.actionposreset;
                case "wf_act_moverotate":
                case "wf_act_move_rotate":
                    return InteractionType.actionmoverotate;
                case "actionresettimer":
                case "wf_act_reset_timers":
                    return InteractionType.actionresettimer;
                case "wf_act_saymsg":
                case "wf_act_show_message":
                    return InteractionType.actionshowmessage;
                case "wf_act_moveuser":
                case "wf_act_teleport_to":
                    return InteractionType.actionteleportto;
                case "wf_act_togglefurni":
                case "wf_act_toggle_state":
                    return InteractionType.actiontogglestate;
                case "wf_cnd_furnis_hv_avtrs":
                    return InteractionType.conditionfurnishaveusers;
                case "conditionstatepos":
                case "wf_cnd_match_snapshot":
                    return InteractionType.conditionstatepos;
                case "conditiontimelessthan":
                case "wf_cnd_time_less_than":
                    return InteractionType.conditiontimelessthan;
                case "conditiontimemorethan":
                case "wf_cnd_time_more_than":
                    return InteractionType.conditiontimemorethan;
                case "wf_cnd_trggrer_on_frn":
                    return InteractionType.conditiontriggeronfurni;
                case "arrowplate":
                    return InteractionType.arrowplate;
                case "preassureplate":
                    return InteractionType.preassureplate;
                case "ringplate":
                    return InteractionType.ringplate;
                case "colortile":
                    return InteractionType.colortile;
                case "colorwheel":
                    return InteractionType.colorwheel;
                case "floorswitch1":
                    return InteractionType.floorswitch1;
                case "floorswitch2":
                    return InteractionType.floorswitch2;
                case "firegate":
                    return InteractionType.firegate;
                case "glassfoor":
                    return InteractionType.glassfoor;
                case "wf_xtra_random":
                    return InteractionType.specialrandom;
                case "specialunseen":
                case "wf_xtra_unseen":
                    return InteractionType.specialunseen;
                case "wire":
                    return InteractionType.wire;
                case "wireCenter":
                    return InteractionType.wireCenter;
                case "wireCorner":
                    return InteractionType.wireCorner;
                case "wireSplitter":
                    return InteractionType.wireSplitter;
                case "wireStandard":
                    return InteractionType.wireStandard;
                case "puzzlebox":
                    return InteractionType.puzzlebox;
                case "gift":
                    return InteractionType.gift;
                case "mannequin":
                    return InteractionType.mannequin;
                case "guild_item":
                case "gld_item":
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
