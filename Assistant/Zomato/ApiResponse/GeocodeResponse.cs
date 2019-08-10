
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace Assistant.Zomato.ApiResponse {
	public class GeocodeResponse {
		public Location location { get; set; }
		public Popularity popularity { get; set; }
		public string link { get; set; }
		public Nearby_Restaurants[] nearby_restaurants { get; set; }

		public class Location {
			public string entity_type { get; set; }
			public int entity_id { get; set; }
			public string title { get; set; }
			public string latitude { get; set; }
			public string longitude { get; set; }
			public int city_id { get; set; }
			public string city_name { get; set; }
			public int country_id { get; set; }
			public string country_name { get; set; }
		}

		public class Popularity {
			public string popularity { get; set; }
			public string nightlife_index { get; set; }
			public string[] nearby_res { get; set; }
			public string[] top_cuisines { get; set; }
			public string popularity_res { get; set; }
			public string nightlife_res { get; set; }
			public string subzone { get; set; }
			public int subzone_id { get; set; }
			public string city { get; set; }
		}

		public class Nearby_Restaurants {
			public Restaurant restaurant { get; set; }
		}

		public class Restaurant {
			public R R { get; set; }
			public string apikey { get; set; }
			public string id { get; set; }
			public string name { get; set; }
			public string url { get; set; }
			public Location1 location { get; set; }
			public int switch_to_order_menu { get; set; }
			public string cuisines { get; set; }
			public int average_cost_for_two { get; set; }
			public int price_range { get; set; }
			public string currency { get; set; }
			public object[] offers { get; set; }
			public int opentable_support { get; set; }
			public int is_zomato_book_res { get; set; }
			public string mezzo_provider { get; set; }
			public int is_book_form_web_view { get; set; }
			public string book_form_web_view_url { get; set; }
			public string book_again_url { get; set; }
			public string thumb { get; set; }
			public User_Rating user_rating { get; set; }
			public string photos_url { get; set; }
			public string menu_url { get; set; }
			public string featured_image { get; set; }
			public int has_online_delivery { get; set; }
			public int is_delivering_now { get; set; }
			public bool include_bogo_offers { get; set; }
			public string deeplink { get; set; }
			public int is_table_reservation_supported { get; set; }
			public int has_table_booking { get; set; }
			public string events_url { get; set; }
		}

		public class R {
			public Has_Menu_Status has_menu_status { get; set; }
			public int res_id { get; set; }
		}

		public class Has_Menu_Status {
			public int delivery { get; set; }
			public int takeaway { get; set; }
		}

		public class Location1 {
			public string address { get; set; }
			public string locality { get; set; }
			public string city { get; set; }
			public int city_id { get; set; }
			public string latitude { get; set; }
			public string longitude { get; set; }
			public string zipcode { get; set; }
			public int country_id { get; set; }
			public string locality_verbose { get; set; }
		}

		public class User_Rating {
			public int aggregate_rating { get; set; }
			public string rating_text { get; set; }
			public string rating_color { get; set; }
			public Rating_Obj rating_obj { get; set; }
			public int votes { get; set; }
			public string custom_rating_text { get; set; }
			public string custom_rating_text_background { get; set; }
			public string rating_tool_tip { get; set; }
		}

		public class Rating_Obj {
			public Title title { get; set; }
			public Bg_Color bg_color { get; set; }
		}

		public class Title {
			public string text { get; set; }
		}

		public class Bg_Color {
			public string type { get; set; }
			public string tint { get; set; }
		}
	}
}
