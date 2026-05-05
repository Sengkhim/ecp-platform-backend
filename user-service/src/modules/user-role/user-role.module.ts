import { Module } from '@nestjs/common';
import { UserRoleService } from './user-role.service';
import { UserRoleController } from '@/modules/user-role/user-role.controller';

@Module({
    providers: [UserRoleService],
    controllers: [UserRoleController],
})
export class UserRoleModule {}
