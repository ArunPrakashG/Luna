namespace Assistant.Zomato.ApiResponse {
	public class GeocodeResponse {
		public Location? location { get; set; }
		public Popularity? popularity { get; set; }
		public string link { get; set; } = string.Empty;
		public Nearby_Restaurants[]? nearby_restaurants { get; set; }

		public class Location {
			public string entity_type { get; set; } = string.Empty;
			public int entity_id { get; set; }
			public string title { get; set; } = string.Empty;
			public string latitude { get; set; } = string.Empty;
			public string longitude { get; set; } = string.Empty;
			public int city_id { get; set; }
			public string city_name { get; set; } = string.Empty;
			public int country_id { get; set; }
			public string country_name { get; set; } = string.Empty;
		}

		public class Popularity {
			public string popularity { get; set; } = string.Empty;
			public string nightlife_index { get; set; } = string.Empty;
			public string[]? nearby_res { get; set; }
			public string[]? top_cuisines { get; set; }
			public string popularity_res { get; set; } = string.Empty;
			public string nightlife_res { get; set; } = string.Empty;
			public string subzone { get; set; } = string.Empty;
			public int subzone_id { get; set; }
			public string city { get; set; } = string.Empty;
		}

		public class Nearby_Restaurants {
			public Restaurant? restaurant { get; set; }
		}

		public class Restaurant {
			public R? R { get; set; }
			public string apikey { get; set; } = string.Empty;
			public string id { get; set; } = string.Empty;
			public string name { get; set; } = string.Empty;
			public string url { get; set; } = string.Empty;
			public Location1? location { get; set; }
			public int switch_to_order_menu { get; set; }
			public string cuisines { get; set; } = string.Empty;
			public int average_cost_for_two { get; set; }
			public int price_range { get; set; }
			public string currency { get; set; } = string.Empty;
			public object[]? offers { get; set; }
			public int opentable_support { get; set; }
			public int is_zomato_book_res { get; set; }
			public string mezzo_provider { get; set; } = string.Empty;
			public int is_book_form_web_view { get; set; }
			public string book_form_web_view_url { get; set; } = string.Empty;
			public string book_again_url { get; set; } = string.Empty;
			public string thumb { get; set; } = string.Empty;
			public User_Rating? user_rating { get; set; }
			public string photos_url { get; set; } = string.Empty;
			public string menu_url { get; set; } = string.Empty;
			public string featured_image { get; set; } = string.Empty;
			public int has_online_delivery { get; set; }
			public int is_delivering_now { get; set; }
			public bool include_bogo_offers { get; set; }
			public string deeplink { get; set; } = string.Empty;
			public int is_table_reservation_supported { get; set; }
			public int has_table_booking { get; set; }
			public string events_url { get; set; } = string.Empty;
		}

		public class R {
			public Has_Menu_Status? has_menu_status { get; set; }
			public int res_id { get; set; }
		}

		public class Has_Menu_Status {
			public int delivery { get; set; }
			public int takeaway { get; set; }
		}

		public class Location1 {
			public string address { get; set; } = string.Empty;
			public string locality { get; set; } = string.Empty;
			public string city { get; set; } = string.Empty;
			public int city_id { get; set; }
			public string latitude { get; set; } = string.Empty;
			public string longitude { get; set; } = string.Empty;
			public string zipcode { get; set; } = string.Empty;
			public int country_id { get; set; }
			public string locality_verbose { get; set; } = string.Empty;
		}

		public class User_Rating {
			public int aggregate_rating { get; set; }
			public string rating_text { get; set; } = string.Empty;
			public string rating_color { get; set; } = string.Empty;
			public Rating_Obj? rating_obj { get; set; }
			public int votes { get; set; }
			public string custom_rating_text { get; set; } = string.Empty;
			public string custom_rating_text_background { get; set; } = string.Empty;
			public string rating_tool_tip { get; set; } = string.Empty;
		}

		public class Rating_Obj {
			public Title? title { get; set; }
			public Bg_Color? bg_color { get; set; }
		}

		public class Title {
			public string text { get; set; } = string.Empty;
		}

		public class Bg_Color {
			public string? type { get; set; }
			public string? tint { get; set; }
		}
	}
}
