﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : BaseController
    {
        public CoursesController(CTDbContext context, ILogger<CoursesController> logger) : base(context, logger) { }

        /// <summary>
        /// Gets all Courses for departmentId
        /// </summary>
        [HttpGet("ByDepartment/{departmentId}")]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses(string departmentId)
        {
            var courses = await _context.Courses.Where(c => c.DepartmentId == departmentId).OrderBy(c => c.CourseNumber).ToListAsync();

            if (courses.Count == 0)
            {
                return NotFound();
            }

            return courses;
        }

        // GET: api/Courses/5
        /// <summary>
        /// Get course for id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(string id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return course;
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutCourse(string id, Course course)
        {
            if (course == null || id != course.Id)
            {
                return BadRequest();
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Courses
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Course>> PostCourse(Course course)
        {
            if (course == null)
            {
                return BadRequest();
            }

            if (await _context.Courses.AnyAsync(c => c.CourseNumber == course.CourseNumber && c.DepartmentId == course.DepartmentId))
            {
                var existing_course = await _context.Courses.Where(c => c.CourseNumber == course.CourseNumber && c.DepartmentId == course.DepartmentId).FirstAsync();
                return CreatedAtAction("GetCourse", new { id = existing_course.Id }, existing_course);
            }
            else
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetCourse", new { id = course.Id }, course);
            }
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Course>> DeleteCourse(string id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return course;
        }

        private bool CourseExists(string id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
