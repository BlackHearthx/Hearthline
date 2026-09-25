using System;
using System.Collections.Generic;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Player-facing lines in the languages Valheim ships.
	/// English stays the exact string the tests already expect.
	/// Unknown languages fall back to English.
	/// </summary>
	public static class GameText
	{
		public const string English = "English";

		public static readonly string[] Languages =
		{
			"English",
			"Swedish",
			"French",
			"German",
			"Spanish",
			"Russian",
			"Portuguese_Brazilian",
			"Portuguese_European",
			"Japanese",
			"Korean",
			"Chinese",
			"Chinese_Trad",
			"Polish",
			"Turkish",
			"Dutch",
			"Ukrainian",
			"Italian",
			"Czech",
			"Danish",
			"Finnish",
			"Greek",
			"Hungarian",
			"Norwegian",
			"Slovak",
		};

		private static readonly Dictionary<string, string[]> Rows = Build();

		public static string Line(string key, string language = English)
		{
			if (string.IsNullOrEmpty(key) || !Rows.TryGetValue(key, out string[] row))
			{
				return key ?? string.Empty;
			}

			int index = 0;
			if (!string.IsNullOrEmpty(language))
			{
				for (int i = 0; i < Languages.Length; i++)
				{
					if (string.Equals(Languages[i], language, StringComparison.Ordinal))
					{
						index = i;
						break;
					}
				}
			}

			string text = row[index];
			return string.IsNullOrEmpty(text) ? row[0] : text;
		}

		private static Dictionary<string, string[]> Build()
		{
			var map = new Dictionary<string, string[]>(StringComparer.Ordinal);
			foreach (string raw in Table.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
				{
					continue;
				}

				string[] cols = line.Split('|');
				if (cols.Length != Languages.Length + 1)
				{
					throw new InvalidOperationException(
						"GameText row has " + cols.Length + " columns, expected " + (Languages.Length + 1) + ": " + cols[0]);
				}

				var values = new string[Languages.Length];
				Array.Copy(cols, 1, values, 0, Languages.Length);
				map[cols[0]] = values;
			}

			return map;
		}

		// key|English|Swedish|French|German|Spanish|Russian|Portuguese_Brazilian|Portuguese_European|Japanese|Korean|Chinese|Chinese_Trad|Polish|Turkish|Dutch|Ukrainian|Italian|Czech|Danish|Finnish|Greek|Hungarian|Norwegian|Slovak
		private const string Table =
			"put_down|Put down|Sätt ner|Poser|Absetzen|Bajar|Положить|Soltar|Pousar|下ろす|내려놓기|放下|放下|Odłóż|Bırak|Neerzetten|Покласти|Metti giù|Položit|Sæt ned|Laske alas|Άφησε κάτω|Letenni|Sett ned|Položiť\n" +
			"steal_carry|Steal & carry|Ta och bär|Voler et porter|Stehlen und tragen|Robar y cargar|Украсть и нести|Roubar e carregar|Roubar e carregar|盗んで抱える|훔쳐 들기|偷走抱起|偷走抱起|Ukraść i nieść|Çalıp taşı|Stelen en dragen|Вкрасти й нести|Ruba e porta|Ukrást a nést|Stjæl og bær|Varasta ja kanna|Κλέψε και κουβάλα|Ellopni és vinni|Stjel og bær|Ukradnúť a niesť\n" +
			"carry|Carry|Bär|Porter|Tragen|Cargar|Нести|Carregar|Carregar|抱える|들기|抱起|抱起|Nieść|Taşı|Dragen|Нести|Porta|Nésto|Bær|Kanna|Κουβάλα|Vinni|Bær|Niesť\n" +
			"growing|Growing {0}%|Växer {0}%|Grandit {0}%|Wächst {0}%|Creciendo {0}%|Растёт {0}%|Crescendo {0}%|A crescer {0}%|成長 {0}%|성장 {0}%|成长 {0}%|成長 {0}%|Rośnie {0}%|Büyüyor %{0}|Groeit {0}%|Росте {0}%|Cresce {0}%|Roste {0}%|Vokser {0}%|Kasvaa {0}%|Μεγαλώνει {0}%|Nő {0}%|Vokser {0}%|Rastie {0}%\n" +
			"bond|Bond {0}/{1}|Band {0}/{1}|Lien {0}/{1}|Bindung {0}/{1}|Vínculo {0}/{1}|Узы {0}/{1}|Vínculo {0}/{1}|Laço {0}/{1}|絆 {0}/{1}|유대 {0}/{1}|羁绊 {0}/{1}|羈絆 {0}/{1}|Więź {0}/{1}|Bağ {0}/{1}|Band {0}/{1}|Зв'язок {0}/{1}|Legame {0}/{1}|Pouto {0}/{1}|Bånd {0}/{1}|Side {0}/{1}|Δεσμός {0}/{1}|Kötelék {0}/{1}|Bånd {0}/{1}|Puto {0}/{1}\n" +
			"expecting|Expecting|Väntar unge|En attente|Trächtig|Esperando cría|Ждёт детёныша|Grávida|À espera|妊娠中|임신 중|有孕|有孕|W ciąży|Gebe|Drachtig|Вагітна|In attesa|Březí|Drægtig|Tiineenä|Έγκυος|Vemhes|Drektig|Gravidná\n" +
			"expecting_soon|Expecting — any moment|Ungen kan komma nu|Le petit peut naître|Kann jeden Moment kommen|Puede nacer ya|Вот-вот родится|O filhote pode nascer agora|A cria pode nascer agora|今にも生まれそう|곧 태어날 듯|随时会生|隨時會生|Młode może się urodzić|Her an doğurabilir|Kan elk moment werpen|Ось-ось народить|Può nascere ora|Může se narodit hned|Ungen kan komme nu|Pentua voi tulla nyt|Μπορεί να γεννήσει τώρα|Mindjárt ellik|Ungen kan komme nå|Mláďa sa môže narodiť hneď\n" +
			"expecting_due|Expecting — due in {0}|Ungen kommer om {0}|Naît dans {0}|Kommt in {0}|Nace en {0}|Родится через {0}|Nasce em {0}|Nasce em {0}|あと {0}|태어나기까지 {0}|还有 {0}|還有 {0}|Poród za {0}|Doğuma {0}|Werpt over {0}|Народить за {0}|Nasce tra {0}|Narodí se za {0}|Fødes om {0}|Syntyy {0} kuluttua|Γεννά σε {0}|Ellik {0} múlva|Fødes om {0}|Narodí sa o {0}\n" +
			"pen_full|Pen full — no breeding|Fållan är full|Enclos plein|Pferch voll|Corral lleno|Загон полон|Curral cheio|Curral cheio|囲いがいっぱい|우리가 가득 참|围栏已满|圍欄已滿|Zagroda pełna|Ağıl dolu|Ren vol|Загорода повна|Recinto pieno|Ohrada plná|Folden er fuld|Tarha on täynnä|Το μαντρί γέμισε|Az ól tele van|Kveen er full|Ohrada je plná\n" +
			"favorite|Favorite meal — better young chance|Favoritmat — bättre chans på unge|Repas favori — meilleure chance|Lieblingsfutter — bessere Chance|Comida favorita — más suerte con la cría|Любимая еда — выше шанс|Comida favorita — filhote com mais chance|Refeição favorita — mais hipótese de cria|好きな餌 — 子が生まれやすい|좋아하는 먹이 — 새끼 확률이 높아짐|最爱的食物 — 幼崽更容易|最愛的食物 — 幼崽更容易|Ulubione jedzenie — większa szansa|Sevdiği yemek — yavru şansı artar|Lievelingseten — betere kans|Улюблена їжа — більший шанс|Pasto preferito — più possibilità|Oblíbené krmení — lepší šance|Yndlingsmad — bedre chance|Lempiateria — parempi mahdollisuus|Αγαπημένο φαγητό — καλύτερη τύχη|Kedvenc eledel — jobb esély|Yndlingsmat — bedre sjanse|Obľúbené krmivo — lepšia šanca\n" +
			"released|Released|Nedsatt|Posé|Abgesetzt|Bajado|Отпустил|Soltei|Pousei|下ろした|내려놓았다|放下了|放下了|Odłożone|Bıraktın|Neergezet|Поклав|Messo giù|Položeno|Sat ned|Laskettu|Το άφησες|Letetted|Satt ned|Položené\n" +
			"dropped|Dropped — hit!|Tappade — du blev träffad|Tombé — tu as pris un coup|Fallen — getroffen|Se cayó — te golpearon|Выпало — тебя ударили|Caiu — levei um golpe|Caiu — levei um golpe|落とした — 殴られた|떨어뜨림 — 맞았다|松手了 — 被打中|鬆手了 — 被打中|Upadło — dostałeś|Düştü — darbe aldın|Gevallen — je werd geraakt|Впало — тебе вдарили|Caduto — ti hanno colpito|Spadlo — dostal jsi ránu|Tabt — du blev ramt|Pudotit — sinuun osui|Έπεσε — σε χτύπησαν|Elejtetted — eltaláltak|Mistet — du ble truffet|Spadlo — dostal si zásah\n" +
			"took_young|You took the young|Du tog ungen|Tu as pris le petit|Du hast das Junge|Te llevaste la cría|Ты забрал детёныша|Peguei o filhote|Levei a cria|子を抱き上げた|새끼를 데려갔다|抱走了幼崽|抱走了幼崽|Zabrałeś młode|Yavruyu aldın|Je nam het jong mee|Ти забрав маля|Hai preso il piccolo|Vzal jsi mládě|Du tog ungen|Otit poikasen|Πήρες το μικρό|Elvitted a kicsit|Du tok ungen|Zobral si mláďa\n" +
			"carrying_young|Carrying young|Bär en unge|Tu portes le petit|Du trägst das Junge|Llevas la cría|Несёшь детёныша|Carregando o filhote|A carregar a cria|子を抱えている|새끼를 안고 있다|抱着幼崽|抱著幼崽|Niesiesz młode|Yavruyu taşıyorsun|Je draagt het jong|Несеш маля|Porti il piccolo|Neseš mládě|Bærer en unge|Kannat poikasta|Κουβαλάς το μικρό|Viszed a kicsit|Bærer en unge|Nesieš mláďa\n" +
			"carrying|Carrying|Bär|Tu portes|Du trägst|Llevas|Несёшь|Carregando|A carregar|抱えている|안고 있다|抱着|抱著|Niesiesz|Taşıyorsun|Je draagt|Несеш|Porti|Neseš|Bærer|Kannat|Κουβαλάς|Viszed|Bærer|Nesieš\n" +
			"cannot_steal|Cannot steal this|Den går inte att ta|Impossible à voler|Das lässt sich nicht stehlen|No se puede robar|Это не украсть|Não dá para roubar|Não dá para roubar isto|これは盗めない|이건 훔칠 수 없다|这个偷不走|這個偷不走|Tego nie ukradniesz|Bunu çalamazsın|Dit kun je niet stelen|Це не вкрасти|Questo non si ruba|Tohle neukradneš|Den kan ikke stjæles|Tätä ei voi varastaa|Αυτό δεν κλέβεται|Ezt nem lehet ellopni|Denne kan ikke stjeles|Toto neukradneš\n" +
			"steal_failed|Steal failed — not owner yet|Gick inte — inte din än|Raté — pas encore à toi|Ging nicht — noch nicht deins|Falló — aún no es tuyo|Не вышло — ещё не твой|Não deu — ainda não é seu|Falhou — ainda não é teu|失敗 — まだ自分のものじゃない|실패 — 아직 네 것이 아님|没成 — 还不是你的|沒成 — 還不是你的|Nie udało się — jeszcze nie twój|Olmadı — henüz senin değil|Mislukt — nog niet van jou|Не вийшло — ще не твій|Non è riuscito — non è ancora tuo|Nešlo to — ještě není tvůj|Mislykkedes — ikke din endnu|Ei onnistunut — ei vielä sinun|Απέτυχε — δεν είναι ακόμα δικό σου|Nem sikerült — még nem a tiéd|Mislyktes — ikke din ennå|Nepodarilo sa — ešte nie je tvoje\n" +
			"only_tamed|Only tamed adults|Bara tämda vuxna|Seulement les adultes apprivoisés|Nur zahme Erwachsene|Solo adultos domados|Только прирученные взрослые|Só adultos domados|Só adultos domados|なついた大人だけ|길들인 어른만|只有驯服的成年|只有馴服的成年|Tylko oswojone dorosłe|Yalnız evcil yetişkinler|Alleen tamme volwassenen|Лише приручені дорослі|Solo adulti addomesticati|Jen ochočení dospělí|Kun tamme voksne|Vain kesyt aikuiset|Μόνο ήμερα ενήλικα|Csak szelíd felnőttek|Bare tamme voksne|Len skrotené dospelé\n" +
			"steal_cooldown|Steal cooldown|Vänta lite|Attends un peu|Warte noch|Espera un poco|Подожди немного|Espera um pouco para roubar de novo|Espera um pouco para roubar outra vez|少し待って|조금만 기다려|再偷要等一等|再偷要等一等|Odczekaj chwilę|Biraz bekle|Wacht even|Зачекай трохи|Aspetta un po'|Počkej chvíli|Vent lidt|Odota hetki|Περίμενε λίγο|Várj egy kicsit|Vent litt|Počkaj chvíľu\n" +
			"too_tired|Too tired to steal|För trött för att ta den|Trop fatigué pour voler|Zu müde zum Stehlen|Muy cansado para robar|Слишком устал, чтобы красть|Sem fôlego para roubar|Sem fôlego para roubar|疲れて盗めない|너무 지쳐서 못 훔친다|太累了，偷不动|太累了，偷不動|Za zmęczony, by kraść|Çalamayacak kadar yorgun|Te moe om te stelen|Занадто втомлений, щоб красти|Troppo stanco per rubare|Příliš unavený na krádež|For træt til at stjæle|Liian väsynyt varastamaan|Πολύ κουρασμένος για κλοπή|Túl fáradt a lopáshoz|For trøtt til å stjele|Príliš unavený na krádež\n" +
			"young|Young|Unge|Jeune|Junges|Cría|Детёныш|Filhote|Cria|子|새끼|幼崽|幼崽|Młode|Yavru|Jong|Маля|Piccolo|Mládě|Unge|Poikanen|Μικρό|Kicsi|Unge|Mláďa\n" +
			"boar_piggy|Boar piggy|Vildsvinskulting|Marcassin|Frischling|Jabato|Поросёнок|Leitão|Leitão|イノシシの子|새끼 멧돼지|小野猪|小野豬|Warchlak|Yavru domuz|Biggetje|Порося|Cinghialino|Selátko|Vildsvinegris|Villisian porsas|Γουρουνάκι|Vaddisznómalac|Villsvingris|Ciciak\n" +
			"wolf_cub|Wolf cub|Vargunge|Louveteau|Wolfswelpe|Cachorro de lobo|Волчонок|Filhote de lobo|Cria de lobo|オオカミの子|새끼 늑대|小狼|小狼|Szczenię wilka|Kurt yavrusu|Wolfjong|Вовченя|Cucciolo di lupo|Vlče|Ulvehvalp|Sudenpentu|Λυκόπουλο|Farkaskölyök|Ulvevalp|Vlča\n" +
			"lox_calf|Lox calf|Loxkalv|Jeune lox|Loxkalb|Cría de lox|Детёныш локса|Filhote de lox|Cria de lox|ロックスの子|새끼 록스|幼年洛克斯|幼年洛克斯|Młody lox|Lox yavrusu|Loxkalf|Маля локса|Piccolo lox|Mládě loxe|Loxkalv|Loxin vasikka|Μικρό λοξ|Loxborjú|Loxkalv|Mláďa loxu\n" +
			"moose_calf|Moose calf|Älgkalv|Faon d'élan|Elchkalb|Cría de alce|Лосёнок|Filhote de alce|Cria de alce|ヘラジカの子|새끼 무스|小驼鹿|小駝鹿|Łoszak|Geyik yavrusu|Elandkalf|Лосеня|Piccolo alce|Losí mládě|Elgkalv|Hirvenvasa|Ελαφάκι|Jávorszarvasborjú|Elgkalv|Losie mláďa\n" +
			"asksvin_hatchling|Asksvin hatchling|Asksvinunge|Jeune asksvin|Asksvin-Küken|Cría de asksvin|Детёныш асксвина|Filhote de asksvin|Cria de asksvin|アスクスヴィンの子|새끼 아스크스빈|小阿斯克温|小阿斯克溫|Młody asksvin|Asksvin yavrusu|Asksvinjong|Маля асксвіна|Piccolo asksvin|Mládě asksvinu|Asksvinunge|Asksvinin poikanen|Μικρό άσκσβιν|Asksvinfióka|Asksvinunge|Mláďa asksvinu\n" +
			"chicken|Chicken|Kyckling|Poussin|Küken|Polluelo|Цыплёнок|Pintinho|Pinto|ヒヨコ|병아리|小鸡|小雞|Kurczak|Civciv|Kuiken|Курча|Pulcino|Kuře|Kylling|Tipu|Κοτοπουλάκι|Csibe|Kylling|Kuriatko\n" +
			"boar|Boar|Vildsvin|Sanglier|Wildschwein|Jabalí|Кабан|Javali|Javali|イノシシ|멧돼지|野猪|野豬|Dzik|Yaban domuzu|Everzwijn|Кабан|Cinghiale|Divočák|Vildsvin|Villisika|Αγριογούρουνο|Vaddisznó|Villsvin|Diviak\n" +
			"wolf|Wolf|Varg|Loup|Wolf|Lobo|Волк|Lobo|Lobo|オオカミ|늑대|狼|狼|Wilk|Kurt|Wolf|Вовк|Lupo|Vlk|Ulv|Susi|Λύκος|Farkas|Ulv|Vlk\n" +
			"lox|Lox|Lox|Lox|Lox|Lox|Локс|Lox|Lox|ロックス|록스|洛克斯|洛克斯|Lox|Lox|Lox|Локс|Lox|Lox|Lox|Lox|Λοξ|Lox|Lox|Lox\n" +
			"hen|Hen|Höna|Poule|Henne|Gallina|Курица|Galinha|Galinha|メンドリ|암탉|母鸡|母雞|Kura|Tavuk|Kip|Курка|Gallina|Slepice|Høne|Kana|Κότα|Tyúk|Høne|Sliepka\n";
	}
}
