﻿using Microsoft.AspNetCore.Mvc;
using DanGame.Models;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Mvc.Core;
using X.PagedList;
using Microsoft.Extensions.Hosting;
//using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using DanGame.Extensions;
namespace DenGame.Controllers
{
	public class ForumController : Controller
	{
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<ForumController> _logger;
		private readonly DanGameContext _context;


		public ForumController(IWebHostEnvironment env, ILogger<ForumController> logger, DanGameContext context)
		{
			_env = env;
			_logger = logger;
			_context = context;

		}
		//---------------論壇首頁 有做分頁-------------
		public async Task<IActionResult> Index(string category = "全部主題", int page = 1)
		{
			int pageNumber = page;
			int pageSize = 5;

			var article = (from a in _context.ArticleLists
						   where a.ArticleCategory == category || category == "全部主題"
						   orderby a.ArticalId descending
						   select a)
						   .Include(a => a.ArticalComments)
						   .ThenInclude(a => a.ArticalCommentReplies)
						   .Include(a => a.ArticalViews)
						   .Include(u => u.User)
						   .ThenInclude(u => u.UserProfile)
						   .ToPagedList(pageNumber, pageSize);

			var commentCounts = new Dictionary<int, int>();
			var replyCounts = new Dictionary<int, int>();
			var totalCounts = new Dictionary<int, int>();
			var likeCounts = new Dictionary<int, int>();
			foreach (var articleA in article)
			{
				int commentCount = articleA.ArticalComments.Count;
				int replyCount = articleA.ArticalComments.Sum(c => c.ArticalCommentReplies.Count);
				int likeCount = await _context.ArticalLikes.CountAsync(l => l.ArticalId == articleA.ArticalId);
				int totalCount = commentCount + replyCount;
				commentCounts[articleA.ArticalId] = commentCount;
				replyCounts[articleA.ArticalId] = replyCount;
				likeCounts[articleA.ArticalId] = likeCount;
				totalCounts[articleA.ArticalId] = totalCount;
			}
			var popularArticle = await (from v in _context.ArticalViews
										join l in _context.ArticleLists on v.ArticalId equals l.ArticalId
										group new { v, l } by new { v.ArticalId, l.ArticalTitle, l.ArticalContent } into g
										select new PopularArticleViewModel
										{
											ArticalID = g.Key.ArticalId,
											Amount = g.Count(),
											ArticalTitle = g.Key.ArticalTitle,
											ArticalContent = g.Key.ArticalContent
										})
									   .OrderByDescending(g => g.Amount)
									   .Take(3)
									   .ToListAsync();
			
			// 查询每个用户的文章数量并取前3名
			var oneWeekAgo = DateTime.Now.AddDays(-7);
			var topuser = await (from a in _context.ArticleLists
								 where a.ArticalCreateDate >= oneWeekAgo
								 group a by a.User into g
								 select new TopUserViewModel
								 {
									 UserId = g.Key.UserId,
									 UserName = g.Key.UserName,
									 ArticleCount = g.Count(),
									 ProfilePictureUrl = (from profile in _context.UserProfiles
														  where profile.UserId == g.Key.UserId
														  select profile.ProfilePictureUrl).FirstOrDefault()
								 })
								.OrderByDescending(u => u.ArticleCount)
								.Take(3)
								.ToListAsync();
		
			var viewModel = new PageListViewModel
			{
				ArticleList = article,
				ArticalComments = await _context.ArticalComments.ToListAsync(),
				ArticalViews = await _context.ArticalViews.ToListAsync(),
				TopUsers = topuser,
				PopularArticle = popularArticle,
				CommentCounts = commentCounts,
				ReplyCounts = replyCounts,
				TotalCounts = totalCounts,
				LikeCounts = likeCounts

			};
			return View(viewModel);
		}
		//----------------快來發文吧------------------

		public IActionResult Post()
		{
			var userId = HttpContext.Session.GetInt32("UserId");

			if (!userId.HasValue)
			{
				return RedirectToAction("Login", "User");
			}
			//var userIds = (HttpContext.Session.GetString("UserId"));
			//if (string.IsNullOrEmpty(userIds) || !int.TryParse(userIds, out int userId))
			//{
			//	return RedirectToAction("Login", "User"); // 如果沒有登入則重定向到登入頁面
			//}
			return View();
		}
		//-----------------文章細節---------------------
		public async Task<IActionResult> Artical(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}
			var article = await (from a in _context.ArticleLists
								 where a.ArticalId == id
								 select a)
								.Include(a => a.ArticalComments)
									.ThenInclude(c => c.User)
									.ThenInclude(u => u.UserProfile)
								.Include(a => a.ArticalComments)
									.ThenInclude(c => c.ArticalCommentReplies)
									.ThenInclude(r => r.User)
									.ThenInclude(u => u.UserProfile)
								.Include(a => a.ArticalComments)
									.ThenInclude(c => c.ArticalCommentLikes)
								.FirstOrDefaultAsync();
			if (article == null)
			{
				return NotFound();
			}
			var user = await (from u in _context.Users
							  where u.UserId == article.UserId
							  select u).FirstOrDefaultAsync();
			var userProfile = await (from p in _context.UserProfiles
									 where p.UserId == article.UserId
									 select p).FirstOrDefaultAsync();
			var likes = await (from l in _context.ArticalLikes
							   where l.ArticalId == id
							   select l).ToListAsync();
			var views = await (from v in _context.ArticalViews
							   where v.ArticalId == id
							   select v).ToListAsync();
			var commentLikes = await (from cl in _context.ArticalCommentLikes
									  where article.ArticalComments.Select(c => c.CommentId).Contains(cl.CommentId)
									  select cl).ToListAsync();

			var viewModel = new ArticlePageViewModel
			{
				Article = article,
				User = user,
				UserProfile = userProfile,
				Likes = likes,
				Comments = article.ArticalComments.ToList(),
				Replies = article.ArticalComments.SelectMany(c => c.ArticalCommentReplies).ToList(),
				CommentLikes = commentLikes,
				Views = views
			};
			return View(viewModel);
		}
		//------------------個人主頁---------------------

		public async Task<IActionResult> ForumUser(int id)
		{
			//var userIds = (HttpContext.Session.GetString("UserId"));
			//if (string.IsNullOrEmpty(userIds) || !int.TryParse(userIds, out int userId))
			//{
			//	return RedirectToAction("Login", "User"); // 如果沒有登入則重定向到登入頁面
			//}
			var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
			var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == id);
			var forumuser = await _context.ArticleLists.Where(x => x.UserId == id).ToListAsync();
			var comment = await _context.ArticalComments.Where(x => x.UserId == id).ToListAsync();
			var like = await _context.ArticalLikes.Where(x => x.UserId == id).ToListAsync();
			var commentlike = await _context.ArticalCommentLikes.Where(x => x.UserId == id).ToListAsync();
			var likedArticles = await _context.ArticalLikes
			.Where(like => like.UserId == id)
			.Select(like => like.Artical)
			.ToListAsync();
			if (user == null)
			{
				return NotFound();
			}
			var viewModel = new UserArticlesViewModel
			{
				User = user,
				UserProfile = userProfile,
				Articles = forumuser,
				Likes = like,
				Comments = comment,
				CommentLikes = commentlike,
				LikedArticles = likedArticles

			};

			return View(viewModel);
		}
		//--------------會員個人主頁----------------
		public async Task<IActionResult> ForumUserPersonal()
		{
			var userId = HttpContext.Session.GetInt32("UserId");

			if (!userId.HasValue)
			{
				return RedirectToAction("Login", "User");
			}
			var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
			var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == userId);
			var forumuser = await _context.ArticleLists.Where(x => x.UserId == userId).ToListAsync();
			var comment = await _context.ArticalComments.Where(x => x.UserId == userId).ToListAsync();
			var like = await _context.ArticalLikes.Where(x => x.UserId == userId).ToListAsync();
			var commentlike = await _context.ArticalCommentLikes.Where(x => x.UserId == userId).ToListAsync();
			var likedArticles = await _context.ArticalLikes
			.Where(like => like.UserId == userId)
			.Select(like => like.Artical)
			.ToListAsync();
			if (user == null)
			{
				return NotFound();
			}
			var viewModel = new UserArticlesViewModel
			{
				User = user,
				UserProfile = userProfile,
				Articles = forumuser,
				Likes = like,
				Comments = comment,
				CommentLikes = commentlike,
				LikedArticles = likedArticles

			};

			return View(viewModel);
		}
		//------------------發表上傳文章-----------------
		[HttpGet]
		public IActionResult Upload()
		{
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file, string title, string description, string Category)
		{
			var userIds = (HttpContext.Session.GetString("UserId"));
			if (string.IsNullOrEmpty(userIds) || !int.TryParse(userIds, out int userId))
			{
				return RedirectToAction("Login", "User"); // 如果沒有登入則重定向到登入頁面
			}
			if (file != null && file.Length > 0)
			{
				using (var memoryStream = new MemoryStream())
				{
					await file.CopyToAsync(memoryStream);
					var artical = new ArticleList
					{
						UserId = 5,
						ArticalCoverPhoto = memoryStream.ToArray(),
						ArticalTitle = title,
						ArticalContent = description,
						ArticleCategory = Category
					};

					_context.ArticleLists.Add(artical);
					await _context.SaveChangesAsync();
				}

				return RedirectToAction("Index");
			}

			return View();
		}
		//-----------------ckeditor5上傳照片--------------
		[HttpGet]
		public IActionResult UploadImage()
		{
			return View();
		}
		[HttpPost]
		public IActionResult UploadImage(List<IFormFile> files)
		{
			var filepath = "";
			foreach (IFormFile photo in Request.Form.Files)
			{
				string serverMapPath = Path.Combine(_env.WebRootPath, "images", photo.FileName);
				using (var stream = new FileStream(serverMapPath, FileMode.Create))
				{ photo.CopyTo(stream); }
				filepath = "http://localhost:5000/" + "images/" + photo.FileName;
			}
			return Json(new { url = filepath });
		}

		//------------------編輯文章-------------------
		public IActionResult Edit(int id)
		{
			var edit = _context.ArticleLists.Find(id);
			if (edit == null)
			{
				return NotFound();
			}
			return View(edit);
		}
		[HttpPost]
		public async Task<IActionResult> Edit(ArticleList model)
		{

			var article = await _context.ArticleLists.FindAsync(model.ArticalId);
			if (article == null)
			{
				return NotFound();
			}

			// 更新文章屬性
			article.ArticalTitle = model.ArticalTitle;
			article.ArticalContent = model.ArticalContent;
			article.ArticalCreateDate = DateTime.Now;

			// 處理文件上傳
			if (model.File != null && model.File.Length > 0)
			{
				using (var memoryStream = new MemoryStream())
				{
					await model.File.CopyToAsync(memoryStream);
					article.ArticalCoverPhoto = memoryStream.ToArray();
				}
			}

			try
			{
				_context.Update(article);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				// 捕捉錯誤訊息
				ModelState.AddModelError("", $"無法保存更改: {ex.Message}");
				return View(model);
			}

			return RedirectToAction("ForumUser");

		}
		//-------------------刪除文章頁面-------------------------
		public IActionResult DeleteArticle(int? id)
		{
			var article = _context.ArticleLists.Find(id);
			return View(article);
		}
		[HttpPost]
		//-------------------刪除文章---------------------------
		public IActionResult DeleteArticle(int ArticalId)
		{

			var article = _context.ArticleLists.Find(ArticalId);
			if (article == null)
			{
				return NotFound();
			}
			_context.ArticleLists.Remove(article);
			_context.SaveChanges();
			return Redirect("/Forum/ForumUser");
		}

		//-------------------留言------------------------------
		[HttpPost]
		public async Task<IActionResult> AddComment(string comment, int articalId)
		{
			var userId = HttpContext.Session.GetInt32("UserId");

			if (!userId.HasValue)
			{
				return RedirectToAction("Login", "User");
			}
			if (ModelState.IsValid)
			{
				var newComment = new ArticalComment
				{
					UserId = userId,
					ArticalId = articalId,
					CommentContent = comment,
					CommentCreateDate = DateTime.Now
				};

				_context.ArticalComments.Add(newComment);
				await _context.SaveChangesAsync();

			}

			return RedirectToAction("Index");
		}

		//-------------------回覆------------------------------
		[HttpPost]
		public async Task<IActionResult> AddReply(string comment, int commentId)
		{
			var userId = HttpContext.Session.GetInt32("UserId");

			if (!userId.HasValue)
			{
				return RedirectToAction("Login", "User");
			}
			if (ModelState.IsValid)
			{
				var replyComment = new ArticalCommentReply
				{
					UserId = userId,
					CommentId = commentId,
					ReplyContent = comment,
					CreatedAt = DateTime.Now
				};

				_context.ArticalCommentReplies.Add(replyComment);
				await _context.SaveChangesAsync();

			}
			return RedirectToAction("Index");
		}

		//----------------------搜尋文章-------------------
		public IActionResult Search(string searchTerm)
		{
			var articles = _context.ArticleLists
				.Where(a => a.ArticalTitle.Contains(searchTerm) || a.ArticalContent.Contains(searchTerm))
				.ToList();
			return View(articles);
		}
		//---------------------瀏覽文章--------------------
		public async Task<IActionResult> ViewArticle(int id)
		{
			// 檢查用戶是否已經查看過該文章
			var sessionKey = $"viewed_{id}";
			var article = await _context.ArticleLists.FindAsync(id);
			if (HttpContext.Session.GetString(sessionKey) == null)
			{
				
				if (article == null)
				{
					return NotFound();
				}

				// 增加觀看次數
				article.ViewCount++;
				_context.Update(article);
				await _context.SaveChangesAsync();

				// 將已查看標記寫入 Session
				HttpContext.Session.SetString(sessionKey, "true");
				
			}
			return View(article);




		}
	}
}