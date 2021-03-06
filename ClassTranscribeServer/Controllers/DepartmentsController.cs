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
    public class DepartmentsController : BaseController
    {
        public DepartmentsController(CTDbContext context, ILogger<DepartmentsController> logger) : base(context, logger) { }

        // GET: api/Departments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            return await _context.Departments.ToListAsync();
        }

        /// <summary>
        /// Gets all Departments for universityId
        /// </summary>
        [HttpGet("ByUniversity/{universityId}")]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments(string universityId)
        {
            return await _context.Departments.Where(d => d.UniversityId == universityId).OrderBy(d => d.Acronym).ToListAsync();
        }

        // GET: api/Departments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(string id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            return department;
        }

        // PUT: api/Departments/5
        [HttpPut("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutDepartment(string id, Department department)
        {
            if (department == null || id != department.Id)
            {
                return BadRequest();
            }

            _context.Entry(department).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
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

        // POST: api/Departments
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Department>> PostDepartment(Department department)
        {
            if (department == null)
            {
                return BadRequest();
            }

            var existingDepartment = await _context.Departments
                .Where(d => d.Name == department.Name && d.UniversityId == department.UniversityId)
                .FirstOrDefaultAsync();

            if (existingDepartment != null)
            {
                return CreatedAtAction("GetDepartment", new { id = existingDepartment.Id }, existingDepartment);
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDepartment", new { id = department.Id }, department);
        }

        // DELETE: api/Departments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Department>> DeleteDepartment(string id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return department;
        }

        private bool DepartmentExists(string id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
