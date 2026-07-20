import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Member } from 'src/app/_models/member';
import { GalleryItem, ImageItem } from '@ngx-gallery/core';
import { environment } from 'src/environments/environment';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { MessageService } from 'src/app/_services/message.service';
import { Message } from 'src/app/_models/message';
import { PresenceService } from 'src/app/_services/presence.service';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';
import { ToastrService } from 'ngx-toastr';
import { User } from 'src/app/_models/user';
import { take } from 'rxjs';

@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})
export class MemberDetailComponent implements OnInit, OnDestroy {
  @ViewChild('memberTabs', { static: true }) memberTabs?: TabsetComponent;
  member: Member = {} as Member;
  galleryImages: GalleryItem[] = [];
  imageUrl = environment.imageUrl;
  activeTab?: TabDirective;
  messages: Message[] = [];
  user?: User;
  icebreakers: string[] = [];
  isLoadingIcebreakers = false;
  compatibility: { score: number, reason: string } | null = null;
  isLoadingCompatibility = false;
  dateIdeas: Array<{ title: string, description: string, vibe: string }> = [];
  isPlanningDate = false;

  constructor(
    private accountService: AccountService, 
    private route: ActivatedRoute, 
    private messageService: MessageService, 
    public presenceService: PresenceService,
    private memberService: MembersService,
    private toastr: ToastrService,
    private router: Router
  ) {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: user => {
        if (user) this.user = user;
      }
    });
    this.router.routeReuseStrategy.shouldReuseRoute = () => false;
  }

  ngOnInit(): void {
    this.route.data.subscribe({
      next: data => {
        this.member = data['member'];
      this.loadCompatibility();
    }
    });

    this.route.queryParams.subscribe({
      next: params => {
        params['tab'] && this.selectTab(params['tab']);
      }
    });
  }

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
  }

  getImages(): GalleryItem[] {
    if (!this.member || !this.member.photos) return [];

    return this.member.photos.map(photo => {
      let src: string;
      if (photo.url.startsWith('https')) {
        src = photo.url;
      } else {
        const base = environment.imageUrl.replace(/\/$/, '');
        const path = photo.url.replace(/^\/+/, '');
        src = `${base}/${path}`;
      }
      return new ImageItem({ src, thumb: src });
    });
  }

  planDateWithAi() {
  if (!this.member) return;
  this.isPlanningDate = true;

  const matchData = {
    currentUserInterests: (this.user as any)?.interests || 'coffee, technology',
    currentUserLocation: (this.user as any)?.city || 'Beirut',
    targetUserInterests: this.member.interests || 'outdoors, food',
    targetUserLocation: this.member.city || 'Beirut'
  };

  this.memberService.planDate(matchData).subscribe({
    next: ideas => {
      this.dateIdeas = ideas;
      this.isPlanningDate = false;
    },
    error: () => {
      this.isPlanningDate = false;
      this.toastr.error('Failed to generate date ideas.');
    }
  });
}

  selectTab(heading: string) {
    if (this.memberTabs) {
      this.memberTabs.tabs.find(x => x.heading === heading)!.active = true;
    }
  }

  loadMessages() {
    if (this.member) {
      this.messageService.getMessageThread(this.member.userName).subscribe({
        next: messages => this.messages = messages
      });
    }
  }

  onTabActivated(data: TabDirective) {
    this.activeTab = data;
    if (this.activeTab.heading === 'Messages' && this.user) {
      this.messageService.createHubConnection(this.user, this.member.userName);
    } else {
      this.messageService.stopHubConnection();
    }
  }

  // --- ADDED THIS METHOD HERE ---
  addLike(member: Member) {
    this.memberService.addLike(member.userName).subscribe({
      next: () => this.toastr.success('You have liked ' + member.knownAs)
    });
  }

  loadCompatibility() {
  if (!this.member || !this.user) return;
  this.isLoadingCompatibility = true;

  const matchData = {
    currentUserBio: '',
    currentUserInterests: '',
    currentUserLocation: (this.user as any)?.city || '',
    targetUserBio: this.member.introduction || '',
    targetUserInterests: this.member.interests || '',
    targetUserLocation: this.member.city || ''
  };

  this.memberService.getCompatibility(matchData).subscribe({
    next: res => {
      this.compatibility = res;
      this.isLoadingCompatibility = false;
    },
    error: () => this.isLoadingCompatibility = false
  });
}

generateIcebreakers() {
  this.isLoadingIcebreakers = true;
  const matchData = {
    currentUserBio: '',
    currentUserInterests: '',
    targetUserBio: this.member.introduction || '',
    targetUserInterests: this.member.interests || ''
  };

  this.memberService.getIcebreakers(matchData).subscribe({
    next: lines => {
      this.icebreakers = lines;
      this.isLoadingIcebreakers = false;
    },
    error: () => this.isLoadingIcebreakers = false
  });
}
}