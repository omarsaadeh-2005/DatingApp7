import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Member } from 'src/app/_models/member';
import { Pagination } from 'src/app/_models/pagination';
import { UserParams } from 'src/app/_models/userParams';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {
  members: Member[] = [];
  allMembersBackup: Member[] = []; // Backup copy of full page list
  pagination: Pagination | undefined;
  userParams: UserParams | undefined;
  
  // AI Filter State
  aiSearchQuery = '';
  isAiSearching = false;

  genderList = [
    { value: 'male', display: 'Males' },
    { value: 'female', display: 'Females' }
  ];

  constructor(
    private memberService: MembersService, 
    private toastr: ToastrService
  ) { 
    this.userParams = this.memberService.getUserParams();
  }

  ngOnInit(): void {
    this.loadMembers();
  }

  loadMembers() {
    if (this.userParams) {
      this.memberService.setUserParams(this.userParams);
      this.memberService.getMembers(this.userParams).subscribe({
        next: response => {
          if (response.result && response.pagination) {
            this.members = response.result;
            this.allMembersBackup = [...response.result]; // Keep master backup
            this.pagination = response.pagination;
          }
        }
      });
    }
  }

  resetFilters() {
    this.aiSearchQuery = '';
    this.userParams = this.memberService.resetUserParams();
    this.loadMembers();
  }

  pageChanged(event: any) {
    if (this.userParams && this.userParams?.pageNumber !== event.page) {
      this.userParams.pageNumber = event.page;
      this.memberService.setUserParams(this.userParams);
      this.loadMembers();
    }
  }

  // --- True Gemini AI Filter Method ---
  filterWithAi() {
    if (!this.aiSearchQuery.trim()) {
      this.members = [...this.allMembersBackup]; // Restore full list if search is cleared
      return;
    }

    this.isAiSearching = true;

    // Send query and candidate backup list to Gemini AI via service
    this.memberService.searchMembersWithAi(this.aiSearchQuery, this.allMembersBackup).subscribe({
      next: (matchedUsernames) => {
        this.isAiSearching = false;

        if (matchedUsernames && matchedUsernames.length > 0) {
          // Filter to only display usernames identified by Gemini
          this.members = this.allMembersBackup.filter(m => 
            matchedUsernames.map(u => u.toLowerCase()).includes(m.userName.toLowerCase())
          );
        } else {
          this.members = [];
          this.toastr.info('No members matched your AI search prompt.');
        }
      },
      error: () => {
        this.isAiSearching = false;
        // Fallback: local keyword match if API call fails
        this.fallbackLocalFilter();
      }
    });
  }

  // Fallback matching logic
  private fallbackLocalFilter() {
    const query = this.aiSearchQuery.toLowerCase();
    this.members = this.allMembersBackup.filter(m =>
      (m.city && m.city.toLowerCase().includes(query)) ||
      (m.country && m.country.toLowerCase().includes(query)) ||
      (m.introduction && m.introduction.toLowerCase().includes(query)) ||
      (m.interests && m.interests.toLowerCase().includes(query))
    );
  }
}